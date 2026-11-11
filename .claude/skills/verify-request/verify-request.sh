#!/bin/bash
# 確認依頼を作るスクリプト
#
# 成果物の大きさと SHA256 を**このスクリプトが計算**して埋める。
# 依頼書の数字を手で写して間違える、という事故を構造的に消すのが目的
# （実績: 大きさ 2 回・スタートメニューのパス 1 回、間違えた）。
#
# 使い方: ./verify-request.sh <題名> [--run <run_id>] [--workflow <名前>]
set -u

PROJECT_DIR="$PWD"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TEMPLATE="$SCRIPT_DIR/template.md"
OUT_DIR="$PROJECT_DIR/tmp/確認"
FB_OLD="$PROJECT_DIR/tmp/フィードバック/old"
TODAY=$(date +%F)

TITLE="${1:-}"
if [ -z "$TITLE" ] || [ "${TITLE#--}" != "$TITLE" ]; then
  echo "使い方: $0 <題名> [--run <run_id>] [--workflow <名前>]"
  echo "例:     $0 アイコンの非同期化"
  exit 1
fi
shift

RUN_ID=""; WORKFLOW=""
while [ $# -gt 0 ]; do
  case "$1" in
    --run|--workflow)
      # 値が無いと shift 2 が失敗して引数が減らず、無限ループになる
      [ $# -ge 2 ] || { echo "エラー: $1 に値が無い"; exit 1; }
      [ "$1" = "--run" ] && RUN_ID="$2" || WORKFLOW="$2"
      shift 2 ;;
    *) echo "不明な引数: $1"; exit 1 ;;
  esac
done

[ -f "$TEMPLATE" ] || { echo "エラー: $TEMPLATE が無い"; exit 1; }
mkdir -p "$OUT_DIR"

WARN=()
need() { WARN+=("$1"); }

# --- CI の run を決める ---------------------------------------------------
if ! command -v gh >/dev/null 2>&1; then
  need "gh が無いので run と成果物を取れなかった。★ 手で埋める前に、必ず成果物を落として計算すること"
  RUN_ID="★要取得"; SHA="★要取得"
else
  if [ -z "$RUN_ID" ]; then
    # 別ブランチや別ワークフローの run を拾わないよう、現在のブランチ（と指定があればワークフロー）に絞る
    BRANCH=$(git branch --show-current 2>/dev/null || true)
    LIST_ARGS=(--limit 20 --status success --json databaseId)
    [ -n "$BRANCH" ] && LIST_ARGS+=(--branch "$BRANCH")
    [ -n "$WORKFLOW" ] && LIST_ARGS+=(--workflow "$WORKFLOW")
    RUN_ID=$(gh run list "${LIST_ARGS[@]}" --jq '.[0].databaseId' 2>/dev/null || true)
  fi
  if [ -z "$RUN_ID" ] || [ "$RUN_ID" = "null" ]; then
    need "緑の run が見つからない（ブランチ: ${BRANCH:-不明}）。CI が通ってから出すこと"
    RUN_ID="★要取得"; SHA="★要取得"
  else
    FULL_SHA=$(gh run view "$RUN_ID" --json headSha --jq '.headSha' 2>/dev/null || true)
    if [ -n "$FULL_SHA" ]; then SHA=$(printf '%s' "$FULL_SHA" | cut -c1-7)
    else SHA="★要取得"; need "headSha が取れなかった"; fi
  fi
fi

# --- 未 push の変更が無いか（依頼書の対象がローカルとずれるのを防ぐ） ----
if command -v git >/dev/null 2>&1; then
  # tmp/ はこのスクリプト自身が書き出す場所なので、汚れの判定から外す
  if [ -n "$(git status --porcelain -- . ':(exclude)tmp' 2>/dev/null)" ]; then
    need "作業ツリーが汚れている。★ 依頼書の対象コミットと手元が違う状態で出すと、結果が読めない"
  fi
  # 短縮 SHA は桁数が環境で変わるので、全桁で比べる
  LOCAL_SHA=$(git rev-parse HEAD 2>/dev/null || true)
  if [ -n "$LOCAL_SHA" ] && [ -n "${FULL_SHA:-}" ] && [ "$LOCAL_SHA" != "$FULL_SHA" ]; then
    need "手元の HEAD は $(printf '%s' "$LOCAL_SHA" | cut -c1-7) で、run の headSha は $SHA。★ 文書だけのコミットが載っているなら「実行ファイルは変わりません」と明記する"
  fi
fi

# --- 成果物を落として、大きさと SHA256 を自分で計算 ----------------------
TABLE=""
if [ "$RUN_ID" != "★要取得" ]; then
  NAMES=$(gh api "repos/{owner}/{repo}/actions/runs/$RUN_ID/artifacts" \
            --jq '.artifacts[].name' 2>/dev/null || true)
  if [ -z "$NAMES" ]; then
    need "成果物の一覧が取れなかった"
  fi
  TMP=$(mktemp -d)
  while IFS= read -r name; do
    [ -n "$name" ] || continue
    rm -rf "$TMP/a"
    if ! gh run download "$RUN_ID" -n "$name" -D "$TMP/a" >/dev/null 2>&1; then
      need "成果物 $name を落とせなかった"
      TABLE="$TABLE
  - \`$name\` — ★要取得"
      continue
    fi
    # 中の各ファイルを列挙（配るものが 1 本でも、増えたら気づけるように全部出す）
    while IFS= read -r f; do
      # ★ 桁区切りは printf の %'d に頼らない（ロケール依存で、C だと効かない）
      sz=$(stat -c%s "$f" | sed -e :a -e 's/\(.*[0-9]\)\([0-9]\{3\}\)/\1,\2/;ta')
      h=$(sha256sum "$f" | cut -d' ' -f1)
      rel=${f#"$TMP/a/"}
      TABLE="$TABLE
  - \`$name\` / \`$rel\` — **$sz** バイト / \`$h\`"
    done < <(find "$TMP/a" -type f | sort)
  done <<< "$NAMES"
  rm -rf "$TMP"
fi
[ -n "$TABLE" ] || TABLE="
  - ★要取得（成果物を落として、大きさと SHA256 を計算してから書く）"

# --- 前回のフィードバックと、その持ち越し -------------------------------
PREV="（無し）"; CARRY="（無し）"
if [ -d "$FB_OLD" ]; then
  LAST=$(ls -t "$FB_OLD"/*.md 2>/dev/null | head -1 || true)
  if [ -n "${LAST:-}" ]; then
    PREV="\`tmp/フィードバック/old/$(basename "$LAST")\`"
    # 見出しは template.md「報告の書式」で決めてある。次の見出しの手前まで、コメント行は除いて取る
    C=$(awk '/^## /{on = ($0 ~ /^## *未了・判定できなかった項目/); next} on' "$LAST" \
          | sed -e '/^<!--.*-->$/d' -e '/^$/d' || true)
    [ -n "$C" ] && CARRY="$C"
  fi
fi

# --- 書き出し -----------------------------------------------------------
OUT="$OUT_DIR/確認依頼_${TITLE}_${TODAY}.md"
if [ -e "$OUT" ]; then
  echo "エラー: $OUT は既にある（題名を変えるか、先に old/ へ移す）"
  exit 1
fi

# 値は環境変数で渡す（awk -v はバックスラッシュを解釈してしまう）。
# 置換は正規表現ではなく文字列一致で行う。値に & や \ が含まれても壊れない
V_TITLE="$TITLE" V_TODAY="$TODAY" V_SHA="$SHA" V_RUN="$RUN_ID" \
V_TABLE="${TABLE#$'\n'}" V_PREV="$PREV" V_CARRY="$CARRY" \
awk '
function rep(s, from, to,    out, i) {
  out = ""
  while ((i = index(s, from)) > 0) { out = out substr(s, 1, i - 1) to; s = substr(s, i + length(from)) }
  return out s
}
{
  s = $0
  s = rep(s, "{題名}", ENVIRON["V_TITLE"]);   s = rep(s, "{日付}", ENVIRON["V_TODAY"])
  s = rep(s, "{SHA}", ENVIRON["V_SHA"]);      s = rep(s, "{RUN_ID}", ENVIRON["V_RUN"])
  s = rep(s, "{成果物表}", ENVIRON["V_TABLE"])
  s = rep(s, "- 前回フィードバック: {パス}", "- 前回フィードバック: " ENVIRON["V_PREV"])
  s = rep(s, "{持ち越し}", ENVIRON["V_CARRY"])
  print s
}' "$TEMPLATE" > "$OUT"

echo "作成: ${OUT#$PROJECT_DIR/}"
echo
echo "成果物（このスクリプトが計算した。手で書き換えないこと）:"
printf '%s\n' "$TABLE" | sed '/^$/d'
echo
if [ ${#WARN[@]} -gt 0 ]; then
  echo "★ 埋める前に片付けること:"
  for w in "${WARN[@]}"; do echo "  - $w"; done
  echo
fi
cat <<'EOT'
埋めるときに確かめる 4 つ（rules/verification.md）:
  1. 「測ったこと / 推測していること」を空にしない
  2. 各項目に対照を書く。無ければ「無し（確認のみ）」＋理由
  3. 0 を期待値に置いた項目を表に挙げ、1 になる場面か文字列の在処を添える
  4. パス・キー・数字は、いま製品と成果物から取り直す（前の依頼書から写さない）
EOT
