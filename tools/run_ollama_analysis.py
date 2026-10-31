"""Run the real local model for one analysis, save metadata, then stop its server."""
from pathlib import Path
import json
import os
import subprocess
import sys
import time
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))
from analyze_unity_logs import OLLAMA_MODEL, collect_evidence


def main():
    # Do not download a model for an activation/compilation failure without tests.
    collect_evidence(ROOT / "artifacts/unity")
    binary = os.environ["OLLAMA_BINARY"]
    model = os.environ.get("AI_MODEL", "").strip() or OLLAMA_MODEL
    output = ROOT / "artifacts/ollama"
    output.mkdir(parents=True, exist_ok=True)
    env = {**os.environ, "AI_PROVIDER": "ollama", "OLLAMA_NO_CLOUD": "1",
           "OLLAMA_HOST": "127.0.0.1:11434", "OLLAMA_VULKAN": "0",
           "OLLAMA_MODELS": str(Path(os.environ["RUNNER_TEMP"]) / "dz07-ollama-models")}
    opener = urllib.request.build_opener(urllib.request.ProxyHandler({}))
    with (output / "server.log").open("w", encoding="utf-8") as log:
        server = subprocess.Popen([binary, "serve"], env=env, stdout=log, stderr=subprocess.STDOUT)
        try:
            for _ in range(60):
                if server.poll() is not None:
                    raise RuntimeError("Ollama server exited; see artifacts/ollama/server.log")
                try:
                    with opener.open("http://127.0.0.1:11434/api/version", timeout=2) as response:
                        version = json.load(response)
                    break
                except OSError:
                    time.sleep(1)
            else:
                raise RuntimeError("Ollama server startup timed out")
            with (output / "pull.log").open("w", encoding="utf-8") as pull_log:
                subprocess.run([binary, "pull", model], env=env, stdout=pull_log,
                               stderr=subprocess.STDOUT, check=True, timeout=600)
            with opener.open("http://127.0.0.1:11434/api/tags", timeout=10) as response:
                models = json.load(response)
            (output / "runtime.json").write_text(json.dumps(
                {"server": version, "models": models, "requested_model": model,
                 "inference": "local CPU", "cloud_disabled": True}, indent=2), encoding="utf-8")
            return subprocess.run([sys.executable, str(ROOT / "analyze_unity_logs.py")],
                                  cwd=ROOT, env=env, timeout=950).returncode
        finally:
            server.terminate()
            try:
                server.wait(timeout=15)
            except subprocess.TimeoutExpired:
                server.kill()
                server.wait(timeout=15)


if __name__ == "__main__":
    raise SystemExit(main())
