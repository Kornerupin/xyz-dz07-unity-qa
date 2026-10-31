const fs = require('node:fs');
const path = require('node:path');

module.exports = async ({ github, context, core, evidenceDir = 'artifacts/ai', runAttempt = process.env.GITHUB_RUN_ATTEMPT }) => {
  const status = JSON.parse(fs.readFileSync(path.join(evidenceDir, 'status.json'), 'utf8'));
  if (status.status !== 'ai_success' || status.eligible !== true || status.failed_test_count < 1 ||
      status.provenance?.run_id !== String(context.runId) || status.provenance?.commit !== context.sha ||
      status.provenance?.run_attempt !== String(runAttempt)) {
    throw new Error('AI evidence is not eligible for this commit, run and attempt');
  }
  const report = fs.readFileSync(path.join(evidenceDir, 'ai_bug_report.md'), 'utf8');
  if (!report.trim() || report.length > 60000) throw new Error('Empty or oversized AI report');
  const issue = await github.rest.issues.create({
    owner: context.repo.owner, repo: context.repo.repo,
    title: '[AI QA] Unity test failure - run ' + context.runNumber,
    body: report,
  });
  const result = { url: issue.data.html_url, run_id: String(context.runId),
    run_attempt: String(runAttempt), commit: context.sha };
  fs.writeFileSync(path.join(evidenceDir, 'issue.json'), JSON.stringify(result, null, 2) + '\n');
  core.setOutput('url', result.url);
  await core.summary.addLink('Automatically created AI bug report', result.url).write();
  return result;
};
