"""Local unit tests use synthetic fixtures and fake API responses, not CI evidence."""
from pathlib import Path
import json
import shutil
import uuid
import io
import subprocess
import sys
from types import SimpleNamespace
import unittest
from unittest.mock import Mock

import analyze_unity_logs as analyzer
from tools.set_craft_state import set_state, TEST
from tools.verify_unity_results import verify, EXPECTED

ROOT = Path(__file__).resolve().parents[1]
TEMP_ROOT = ROOT / "artifacts" / "test-temp"
XML = '''<test-run result="Failed"><test-suite><test-case fullname="CraftTest.Test_CraftSword_ConsumesResources" result="Failed"><failure><message>Expected: 4\nBut was: 3</message><stack-trace>at CraftTest.cs:20</stack-trace></failure></test-case></test-suite></test-run>'''
REPORT = {"title": "Несовпадение ожидания теста крафта", "error_type": "AssertionException",
          "root_cause": "Возможное неверное ожидание; проверить код теста и стоимость крафта.",
          "cause_status": "hypothesis", "evidence": ["Expected: 4", "But was: 3"],
          "recommendations": ["Сверить остаток ресурсов с исходным количеством и стоимостью."],
          "uncertainties": "Это синтетический ответ для unit-теста, не результат обращения к модели."}


def fake_factory(content=None, finish="stop", refusal=None, error=None):
    # Explicit test double. This factory is never an option exposed by the CLI.
    if content is None:
        content = json.dumps(REPORT, ensure_ascii=False)
    message = SimpleNamespace(content=content, refusal=refusal)
    response = SimpleNamespace(choices=[SimpleNamespace(message=message, finish_reason=finish)],
        model_dump=lambda **kwargs: {"id": "UNIT-TEST-MOCK-NOT-API", "model": "mock",
                                     "choices": [{"message": {"content": content}, "finish_reason": finish}]})
    create = Mock(side_effect=error, return_value=response)
    factory = Mock(return_value=SimpleNamespace(chat=SimpleNamespace(completions=SimpleNamespace(create=create))))
    return factory


class Workspace(unittest.TestCase):
    def setUp(self):
        TEMP_ROOT.mkdir(parents=True, exist_ok=True)
        # mkdir uses inherited workspace permissions on Windows; mkdtemp's 0700
        # ACL can exclude the sandbox token from its own child directories.
        self.root = TEMP_ROOT / ("case-" + uuid.uuid4().hex)
        self.root.mkdir()
        assert self.root.resolve().is_relative_to(ROOT.resolve())
        self.artifacts = self.root / "unity"
        self.output = self.root / "ai"
        self.artifacts.mkdir()
        self.env = {"AI_PROVIDER": "openai", "AI_API_KEY": "UNIT-TEST-NOT-A-SECRET", "GITHUB_SHA": "mock-commit",
                    "GITHUB_RUN_ID": "42", "GITHUB_REPOSITORY": "test/project",
                    "GITHUB_OUTPUT": str(self.root / "github-output")}

    def tearDown(self):
        assert self.root.resolve().is_relative_to(TEMP_ROOT.resolve())
        shutil.rmtree(self.root)

    def fixture(self, text=XML):
        (self.artifacts / "results.xml").write_text(text, encoding="utf-8")

    def run_case(self, factory=None, env=None):
        return analyzer.run(self.artifacts, self.output, environ=self.env if env is None else env,
                            client_factory=factory or fake_factory(), project_root=self.root)

    def assert_blocked(self):
        status = json.loads((self.output / "status.json").read_text(encoding="utf-8"))
        self.assertFalse(status["eligible"])
        self.assertEqual(status["status"], "blocked")
        self.assertFalse((self.output / "ai_bug_report.md").exists())
        self.assertFalse((self.root / "github-output").exists())


class EvidenceTests(Workspace):
    def test_missing_artifacts_rejected(self):
        with self.assertRaises(analyzer.EvidenceError):
            analyzer.collect_evidence(self.root / "missing", self.root)

    def test_no_logs_no_api_call(self):
        factory = fake_factory()
        self.assertEqual(self.run_case(factory), 1)
        factory.assert_not_called()
        self.assert_blocked()

    def test_error_text_without_failed_test_is_not_evidence(self):
        (self.artifacts / "Editor.log").write_text("License error; cannot open editor")
        self.assertEqual(self.run_case(), 1)
        self.assert_blocked()

    def test_passed_xml_is_not_failure(self):
        self.fixture('<test-run result="Passed"><test-case result="Passed"/></test-run>')
        self.assertEqual(self.run_case(), 1)
        self.assert_blocked()

    def test_invalid_xml_is_not_failure(self):
        self.fixture('<test-run><broken')
        self.assertEqual(self.run_case(), 1)

    def test_failure_without_message_rejected(self):
        self.fixture('<test-run><test-case result="Failed"/></test-run>')
        self.assertEqual(self.run_case(), 1)

    def test_xml_entities_rejected(self):
        self.fixture('<!DOCTYPE test-run [<!ENTITY x "bad">]>' + XML)
        self.assertEqual(self.run_case(), 1)

    def test_failure_preserves_message_and_hash(self):
        self.fixture()
        (self.artifacts / "Editor.log").write_text("UNIT-TEST-FIXTURE only")
        result = analyzer.collect_evidence(self.artifacts, self.root)
        self.assertEqual(result["failed_test_count"], 1)
        self.assertIn("Expected: 4", result["text"])
        self.assertEqual(len(result["sources"][0]["sha256"]), 64)
        self.assertEqual(len(result["engine_logs"]), 1)


class ApiTests(Workspace):
    def setUp(self):
        super().setUp()
        self.fixture()

    def test_missing_key_no_report_or_request(self):
        factory = fake_factory()
        self.assertEqual(self.run_case(factory, {"AI_PROVIDER": "openai"}), 1)
        factory.assert_not_called()
        self.assert_blocked()
        self.assertTrue((self.output / "evidence.json").exists())

    def test_api_failure_never_leaks_exception_body(self):
        self.assertEqual(self.run_case(fake_factory(error=RuntimeError("sensitive-error-body"))), 1)
        self.assert_blocked()
        self.assertNotIn("sensitive-error-body", (self.output / "status.json").read_text())

    def test_empty_response_blocked_and_preserved(self):
        self.assertEqual(self.run_case(fake_factory(content=" ")), 1)
        self.assert_blocked()
        self.assertTrue((self.output / "api_response.json").exists())

    def test_incomplete_response_blocked(self):
        self.assertEqual(self.run_case(fake_factory(finish="length")), 1)
        self.assert_blocked()

    def test_refusal_blocked(self):
        self.assertEqual(self.run_case(fake_factory(refusal="refused")), 1)
        self.assert_blocked()

    def test_invalid_json_blocked(self):
        self.assertEqual(self.run_case(fake_factory(content="not json")), 1)
        self.assert_blocked()

    def test_invented_evidence_blocked(self):
        report = {**REPORT, "evidence": ["Nonexistent ProductionDatabase exploded"]}
        self.assertEqual(self.run_case(fake_factory(content=json.dumps(report))), 1)
        self.assert_blocked()

    def test_empty_recommendations_blocked(self):
        report = {**REPORT, "recommendations": []}
        self.assertEqual(self.run_case(fake_factory(content=json.dumps(report))), 1)
        self.assert_blocked()

    def test_definite_cause_without_uncertainty_blocked(self):
        report = {**REPORT, "cause_status": "confirmed"}
        self.assertEqual(self.run_case(fake_factory(content=json.dumps(report))), 1)
        self.assert_blocked()

    def test_stale_report_removed_before_failure(self):
        self.output.mkdir()
        (self.output / "ai_bug_report.md").write_text("STALE")
        self.assertEqual(self.run_case(env={"AI_PROVIDER": "openai"}), 1)
        self.assert_blocked()

    def test_valid_mock_response_produces_eligible_output(self):
        factory = fake_factory()
        self.assertEqual(self.run_case(factory), 0)
        status = json.loads((self.output / "status.json").read_text())
        self.assertTrue(status["eligible"])
        self.assertEqual(status["provenance"]["run_id"], "42")
        self.assertEqual((self.root / "github-output").read_text().strip(), "eligible=true")
        report = (self.output / "ai_bug_report.md").read_text(encoding="utf-8")
        self.assertIn("hypothesis", report)
        self.assertIn("Expected: 4", report)
        request = factory.return_value.chat.completions.create.call_args.kwargs
        self.assertEqual(request["model"], "gpt-4o-mini")
        self.assertTrue(request["response_format"]["json_schema"]["strict"])


class MutationTests(Workspace):
    def test_green_broken_green_exact_roundtrip(self):
        path = self.root / "CraftTest.cs"
        path.write_bytes(TEST.read_bytes())
        set_state(path, "green")
        original = path.read_bytes()
        green_hash = set_state(path, "green")
        broken_hash = set_state(path, "broken")
        self.assertNotEqual(green_hash, broken_hash)
        self.assertEqual(path.read_text(), original.decode().replace("expectedRemainingWood = 3;", "expectedRemainingWood = 4;"))
        self.assertEqual(set_state(path, "green"), green_hash)
        self.assertEqual(path.read_bytes(), original)

    def test_refuses_unknown_mutation_target_without_writing(self):
        path = self.root / "CraftTest.cs"
        path.write_text("const int expectedRemainingWood = 99;")
        old = path.read_bytes()
        with self.assertRaises(ValueError):
            set_state(path, "broken")
        self.assertEqual(path.read_bytes(), old)

    def test_refuses_duplicate_marker(self):
        path = self.root / "CraftTest.cs"
        path.write_text("const int expectedRemainingWood = 3; // COURSE_MUTATION_POINT\n" * 2)
        with self.assertRaises(ValueError):
            set_state(path, "broken")


class PassingGateTests(Workspace):
    def test_requires_both_passing_tests(self):
        self.fixture('<test-run>' + ''.join(f'<test-case fullname="{name}" result="Passed"/>' for name in EXPECTED) + '</test-run>')
        self.assertEqual(len(verify(self.artifacts)), 2)

    def test_empty_run_does_not_pass(self):
        with self.assertRaises(ValueError):
            verify(self.artifacts)

    def test_skipped_test_does_not_pass(self):
        self.fixture('<test-run>' + ''.join(f'<test-case fullname="{name}" result="Skipped"/>' for name in EXPECTED) + '</test-run>')
        with self.assertRaises(ValueError):
            verify(self.artifacts)

    def test_failed_test_does_not_pass(self):
        self.fixture(XML)
        with self.assertRaises(ValueError):
            verify(self.artifacts)

    def test_other_failed_test_blocks_green_even_when_craft_passes(self):
        self.fixture('<test-run>' + ''.join(f'<test-case fullname="{name}" result="Passed"/>' for name in EXPECTED)
                     + '<test-case fullname="OtherGameTest" result="Failed"/></test-run>')
        with self.assertRaises(ValueError):
            verify(self.artifacts)


class WorkflowTests(unittest.TestCase):
    def test_workflow_is_valid_json_compatible_yaml(self):
        paths = list((ROOT / ".github/workflows").glob("*.yml"))
        self.assertEqual([x.name for x in paths], ["ai_qa_gate.yml"])
        config = json.loads(paths[0].read_text())
        self.assertIn("push", config["on"])
        self.assertEqual(config["jobs"]["ai-report"]["permissions"]["issues"], "write")
        steps = config["jobs"]["craft-gate"]["steps"]
        ids = {step.get("id"): step for step in steps}
        self.assertFalse(any(step.get("continue-on-error") for step in steps))
        self.assertEqual(ids["unity_tests"]["with"]["unityVersion"], "6000.4.0f1")
        self.assertEqual(ids["unity_tests"]["with"]["testMode"], "All")
        # GameCI CLI v0.1.72 rejects the --no-coverageEnabled flag emitted for false.
        self.assertNotIn("coverageEnabled", ids["unity_tests"]["with"])
        self.assertEqual(ids["unity_tests"]["with"]["cliVersion"], "v0.1.72")
        self.assertEqual(ids["unity_tests"]["uses"],
                         "game-ci/unity-test-runner@32e57712352b500e17974b245a6dce9e11a73213")
        self.assertNotIn("customParameters", ids["unity_tests"]["with"])
        self.assertEqual(ids["failure_evidence"]["if"], "${{ failure() && steps.unity_tests.outcome == 'failure' }}")
        job = config["jobs"]["ai-report"]
        self.assertIn("failure()", job["if"])
        self.assertIn("needs.craft-gate.outputs.test_failure == 'true'", job["if"])
        ids = {step.get("id"): step for step in job["steps"]}
        self.assertIn("--propagate-failure", ids["failed_tests"]["run"])
        self.assertFalse(any(step.get("continue-on-error") for step in job["steps"]))
        self.assertIn("failure()", ids["issue"]["if"])
        self.assertIn("steps.analyze.outcome == 'success'", ids["issue"]["if"])
        self.assertIn("steps.analyze.outputs.eligible == 'true'", ids["issue"]["if"])
        self.assertIn("steps.ollama_install.outcome == 'success'", ids["analyze"]["if"])
        self.assertNotIn("AI_API_KEY", json.dumps(config))
        self.assertEqual(ids["analyze"]["env"]["AI_PROVIDER"], "ollama")
        self.assertIn("create_ai_issue.cjs", ids["issue"]["with"]["script"])
        issue_source = (ROOT / "tools/create_ai_issue.cjs").read_text()
        self.assertIn("context.sha", issue_source)
        self.assertIn("status.provenance?.run_id", issue_source)
        self.assertEqual(steps[-1]["if"], "${{ always() }}")


class LocalModelTests(Workspace):
    def setUp(self):
        super().setUp()
        self.fixture()
        self.env.pop("AI_API_KEY")
        self.env.pop("AI_PROVIDER")

    def run_local(self, report=None, *, done=True, reason="stop", model=None, transport=None, env=None):
        raw = {"model": model or analyzer.OLLAMA_MODEL, "done": done, "done_reason": reason,
               "message": {"content": json.dumps(REPORT if report is None else report)}}
        self.transport = transport or Mock(return_value=io.BytesIO(json.dumps(raw).encode()))
        return analyzer.run(self.artifacts, self.output, environ=self.env if env is None else env,
                            project_root=self.root, ollama_transport=self.transport)

    def test_default_local_provider_needs_no_secret(self):
        self.assertEqual(self.run_local(), 0)
        status = json.loads((self.output / "status.json").read_text())
        self.assertEqual(status["provenance"]["provider"], "ollama")
        request = self.transport.call_args.args[0]
        self.assertEqual(request.full_url, "http://127.0.0.1:11434/api/chat")
        self.assertNotIn("Authorization", request.headers)
        payload = json.loads(request.data)
        evidence = analyzer.collect_evidence(self.artifacts, self.root)
        self.assertEqual(payload["format"], analyzer.grounded_report_schema(evidence))
        self.assertEqual(payload["format"]["properties"]["evidence"]["items"]["enum"],
                         ["Expected: 4", "But was: 3"])
        self.assertFalse(payload["stream"])
        self.assertEqual(payload["options"]["temperature"], 0)
        self.assertEqual(json.loads((self.output / "request.json").read_text()), payload)
        self.assertIn(payload["messages"][1]["content"], (self.output / "prompt.txt").read_text())

    def test_local_invented_quote_does_not_create_report(self):
        self.assertEqual(self.run_local({**REPORT, "evidence": ["invented database error"]}), 1)
        self.assert_blocked()

    def test_source_quote_cannot_replace_actual_failure_evidence(self):
        # A copied test name is real text, but it is not a failure-message quote.
        self.assertEqual(self.run_local({**REPORT, "evidence": ["CraftTest.Test_CraftSword_ConsumesResources"]}), 1)
        self.assert_blocked()

    def test_quote_schema_uses_only_current_run_and_preserves_original_text(self):
        first = analyzer.collect_evidence(self.artifacts, self.root)
        other = {**first, "failed_tests": [{"message": "  Expected: less than 0.0f\n  But was:  17.2068291f\n"}]}
        schema = analyzer.grounded_report_schema(other)
        self.assertEqual(schema["properties"]["evidence"]["items"]["enum"],
                         ["Expected: less than 0.0f", "But was:  17.2068291f"])
        self.assertEqual(analyzer.grounded_report_schema(first)["properties"]["evidence"]["items"]["enum"],
                         ["Expected: 4", "But was: 3"])
        self.assertNotIn("enum", analyzer.SCHEMA["properties"]["evidence"]["items"])

    def test_local_incomplete_output_is_rejected(self):
        self.assertEqual(self.run_local(reason="length"), 1)
        self.assert_blocked()
        self.assertTrue((self.output / "api_response.json").exists())

    def test_local_wrong_model_is_rejected(self):
        self.assertEqual(self.run_local(model="unexpected-cloud-model"), 1)
        self.assert_blocked()

    def test_local_connection_failure_no_fallback(self):
        self.assertEqual(self.run_local(transport=Mock(side_effect=ConnectionError("private-details"))), 1)
        self.assert_blocked()
        self.assertNotIn("private-details", (self.output / "status.json").read_text())

    def test_local_oversized_evidence_no_request(self):
        self.fixture(XML.replace("Expected: 4", "X" * 12001))
        self.assertEqual(self.run_local(), 1)
        self.transport.assert_not_called()
        self.assert_blocked()

    def test_local_invalid_json_no_report(self):
        self.assertEqual(self.run_local(transport=Mock(return_value=io.BytesIO(b"invalid"))), 1)
        self.assert_blocked()

    def test_unsupported_provider_does_not_contact_model(self):
        self.assertEqual(self.run_local(env={"AI_PROVIDER": "unknown"}), 1)
        self.transport.assert_not_called()
        self.assert_blocked()

    def test_failure_gate_only_marks_real_failed_test(self):
        self.assertEqual(analyzer.check_failure(self.artifacts, environ=self.env, project_root=self.root), 0)
        output = self.root / "github-output"
        self.assertEqual(output.read_text(), "test_failure=true\n")
        output.unlink()
        self.fixture('<test-run><test-case result="Passed"/></test-run>')
        self.assertEqual(analyzer.check_failure(self.artifacts, environ=self.env, project_root=self.root), 1)
        self.assertFalse(output.exists())

    def test_propagation_cli_returns_failure_from_xml_and_publishes_flag(self):
        import os
        process = subprocess.run([sys.executable, str(ROOT / "analyze_unity_logs.py"),
                                  "--propagate-failure", "--artifacts", str(self.artifacts)],
                                 env={**os.environ, **self.env}, capture_output=True)
        self.assertEqual(process.returncode, 1)
        self.assertEqual((self.root / "github-output").read_text(), "test_failure=true\n")
        (self.root / "github-output").unlink()
        self.fixture('<test-run result="Passed"/>')
        process = subprocess.run([sys.executable, str(ROOT / "analyze_unity_logs.py"),
                                  "--propagate-failure", "--artifacts", str(self.artifacts)],
                                 env={**os.environ, **self.env}, capture_output=True)
        self.assertEqual(process.returncode, 2)
        self.assertFalse((self.root / "github-output").exists())


if __name__ == "__main__":
    unittest.main()
