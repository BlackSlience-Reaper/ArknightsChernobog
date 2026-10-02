#!/bin/zsh
# 用本机 Codex（ChatGPT.app 自带 CLI）按 assets/<name>.md 生成一张贴图到 art/generated/。
# Usage: run_job.sh <asset-name> [reference-image ...]
set -euo pipefail

ART="${0:A:h:h}"
CODEX="${CODEX_BIN:-/Applications/ChatGPT.app/Contents/Resources/codex}"
name="$1"
shift

job_dir="$ART/codex/runs"
mkdir -p "$job_dir" "$ART/generated"
prompt="$job_dir/$name.prompt.md"
cat "$ART/codex/job_header.md" "$ART/codex/style.md" "$ART/codex/assets/$name.md" > "$prompt"
print '\n最后只回复一行：保存的路径和图片像素尺寸。' >> "$prompt"

typeset -a image_args=()
for ref in "$@"; do
	image_args+=(-i "$ref")
done

"$CODEX" exec -C "$ART" -s workspace-write --skip-git-repo-check \
	-o "$job_dir/$name.last.txt" "${image_args[@]}" - < "$prompt" > "$job_dir/$name.log" 2>&1
print "$name: $(cat "$job_dir/$name.last.txt" 2>/dev/null || echo 'no reply')"
