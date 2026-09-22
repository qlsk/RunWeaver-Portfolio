const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { spawnSync } = require('node:child_process');

const tempRoot = fs.realpathSync(os.tmpdir());
const repo = fs.mkdtempSync(path.join(tempRoot, 'runeweaver-hook-test-'));
const hookDir = path.join(repo, 'hooks');
let checks = 0;

function git(args, ok = true) {
  const result = spawnSync('git', args, {
    cwd: repo,
    encoding: 'utf8',
    env: { ...process.env, GIT_EDITOR: ':' },
  });
  if (result.error) throw result.error;
  if (ok) assert.equal(result.status, 0, result.stderr + result.stdout);
  return result;
}

function check(name, message, accepted, config = []) {
  const messageFile = path.join(repo, 'message.txt');
  fs.writeFileSync(messageFile, message);
  const result = git([...config, 'hook', 'run', 'commit-msg', '--', messageFile], false);
  assert.equal(result.status, accepted ? 0 : 1, `${name}: ${result.stdout}${result.stderr}`);
  assert.equal(fs.readFileSync(messageFile, 'utf8'), message, `${name}: message was modified`);
  checks++;
}

try {
  git(['init', '-q']);
  git(['config', 'user.name', 'Hook Test']);
  git(['config', 'user.email', 'hook-test@example.invalid']);
  git(['-c', 'core.hooksPath=', 'commit', '--allow-empty', '-qm', 'initial']);
  const initial = git(['rev-parse', 'HEAD']).stdout.trim();
  fs.mkdirSync(hookDir);
  fs.writeFileSync(path.join(hookDir, 'commit-msg'),
    fs.readFileSync(path.join(__dirname, '..', 'commit-msg'), 'utf8').replace(/\r\n/g, '\n'),
    { mode: 0o755 });
  git(['config', 'core.hooksPath', hookDir.replace(/\\/g, '/')]);

  check('Korean message', 'feat(growth): 성장 기능 추가\n\n경험치를 누적합니다.\n', true);
  check('CRLF message', 'fix: 오류 수정\r\n\r\n참조를 확인합니다.\r\n', true);
  check('Whitespace-only description', 'feat:    \n\n본문\n', false);
  check('Missing body', 'fix: 오류 수정\n', false);
  check('Missing separator', 'fix: 오류 수정\n본문\n', false);
  check('Custom comments removed', 'fix: 오류 수정\n\n; comment\n', false,
    ['-c', 'commit.cleanup=strip', '-c', 'core.commentChar=;']);
  check('Comments before header', '; comment\nfix: 오류 수정\n\n본문\n', true,
    ['-c', 'commit.cleanup=strip', '-c', 'core.commentChar=;']);
  check('Markdown preserved in whitespace mode', 'docs: 설명 추가\n\n# 설명\n', true,
    ['-c', 'commit.cleanup=whitespace']);
  check('Verbatim keeps invalid leading blank', '\nfix: 오류 수정\n\n본문\n', false,
    ['-c', 'commit.cleanup=verbatim']);
  check('Fake merge rejected', "Merge branch 'other'\n", false);
  check('Standard revert', `Revert "initial"\n\nThis reverts commit ${initial}.\n`, true);
  check('Unverifiable revert rejected', `Revert "initial"\n\nThis reverts commit ${'0'.repeat(40)}.\n`, false);
  check('Revert prefix alone rejected', 'Revert "initial"\n\n설명\n', false);

  // Real Git operations verify state detection and generated messages.
  git(['checkout', '-qb', 'other']);
  git(['commit', '--allow-empty', '-qm', 'feat: 다른 기능', '-m', '별도 변경입니다.']);
  git(['checkout', '--detach', initial]);
  git(['merge', '--no-ff', '--no-edit', 'other']);
  checks++;
  fs.writeFileSync(path.join(repo, 'sample.txt'), 'sample\n');
  git(['add', 'sample.txt']);
  git(['commit', '-qm', 'feat: 테스트 파일 추가', '-m', '되돌리기 검증용입니다.']);
  git(['revert', '--no-edit', 'HEAD']);
  checks++;
  console.log(`${checks} commit-msg checks passed.`);
} finally {
  const resolved = fs.realpathSync(repo);
  assert.equal(path.dirname(resolved), tempRoot);
  assert.ok(path.basename(resolved).startsWith('runeweaver-hook-test-'));
  fs.rmSync(resolved, { recursive: true, force: true });
}
