#!/usr/local/bin/pwsh
[string]$commitLintConfig = 
@"
module.exports = { extends: ['@commitlint/config-conventional'] };
"@;

[string]$preCommit = 
@'
#!/bin/sh
. "$(dirname "$0")/_/husky.sh"

npx --no -- commitlint --edit "${1}"
'@

git config advice.ignoreHook false
#git config push.followTags true --global
git config remote.origin.tagopt --tags

if(!(Test-Path $PSScriptRoot/node_modules)) {
    & npm install --save-dev husky
    & npm install --save-dev @commitlint/cli @commitlint/config-conventional
}

if(!(Test-Path $PSScriptRoot/.husky)) {
    & npx husky install

    $commitLintConfig | Out-File -FilePath "$PSSCriptRoot/commitlint.config.js"
    $preCommit | Out-File -FilePath "$PSSCriptRoot/.husky/commit-msg"

    if($IsMacOS -or $IsLinux) {
        & chmod +x $PSScriptRoot/.husky/commit-msg
    }
}