# テストの線引き

**自動で回るもの**と、**実機で人が確かめるもの**の境目をここに書く。
`rules/automated-tests.md` の記録。

このプロジェクトには、テストを回す場所が 3 つある。

| 場所 | 回す人 | 必要なもの |
|---|---|---|
| CI（GitHub Actions） | 自動 | **まだない**。下の「未決」を参照 |
| エージェントが MCP 経由で回す EditMode テスト | エージェント | ホストで開いている Unity エディタと、Unity-MCP の接続（`docs/project_overview.md` の Unity-MCP の節） |
| 人が実機で確かめるもの | 人 | `rules/verification.md` の検証の往復 |

MCP で回すテストは、エディタが開いていてつながっているときにしか走らない。だから CI の代わりにはならない。それでも、門として次の規則を置く。

- **タグを打つ前に、そのパッケージについて次を順に確かめ、すべて満たしていること。** 配布は git URL とタグ（`com.obelune.<機能名>/<バージョン>`）で行うので、タグを打つことが「公開」に当たる
  1. コンソールの最新の番号を控える（`console | sort sequence desc | first 1 | select sequence`）。コンソールはドメインリロードでは消えないので、番号で絞らないと古いエラーと区別できない
  2. `editor | call RefreshAssets()`（`rql-mutate`）を呼ぶ
  3. 控えた番号より新しいエラー（`logType == "Error"`）が 0 件
  4. `tests | where fullName starts-with "Obelune.<機能名>"` の件数が 1 件以上あり、`tests-run` の `TotalTests` と一致する（「何も走っていない」を「全部通った」と取り違えないため）
  5. `FailedTests == 0`

## いまの数字

| | |
|---|---|
| コード | 0 行（パッケージはまだない） |
| 自動テスト | 0 行 / 0 件（タグを打つ前に MCP 経由で走らせる） |
| テストケース一覧 | `docs/**/test-cases.md` |

## MCP 経由の EditMode テストで何ができるか

2026-11-12 に、使い捨てのテスト用 asmdef（`overrideReferences: true`、`precompiledReferences: ["nunit.framework.dll"]`）に、成功するテストと失敗するテストを 1 つずつ置いて測った。計測が終わったら消した。

| 項目 | 結果 | 根拠 |
|---|---|---|
| 成功と失敗を区別して返すか | **測った**: 返す | `tests-run`（`testClass: McpProbeTests`）の結果が `Status: Failed, Total 2, Passed 1, Failed 1`。失敗したテストの `Message` に、仕込んだ目印 `OBELUNE_PROBE_MARKER_7f3a` が入っていた |
| 所要時間 | **測った**: 往復で約 0.6 秒（テスト本体は 0.07 秒） | 呼び出す前の時刻と、サーバーログの `"tests-run" completed` の時刻の差 |
| 新しいファイルの取り込み | **測った**: エージェントが `editor \| call RefreshAssets()`（`rql-mutate`）を呼べば、Unity が取り込んでコンパイルし、`.meta` も作る | `.meta` ができた。`tests \| where fullName contains "McpProbe"` に 2 件出た |
| コンパイルのあとの接続 | **測った**: ドメインリロードのたびに接続は切れるが、7 秒ほどで自動でつながり直す。人が Connect し直す必要はなく、同じセッションのツールもそのまま使える | ソケットの記録: 15:35:14 に切れて 15:35:21 に戻った。15:36:06→15:36:13、15:36:32→15:36:39 も同じ |
| コンパイルエラーの読み取り | **測った**: `console \| where logType == "Error"` でエラーの番号と行が取れる | `error CS0234 ... 'Json' does not exist in the namespace 'System.Text'` |
| asmdef の規則（`project_overview.md`「リポジトリ内の配置」）が依存の書き忘れを止めるか | **測った**: 止める | `System.Text.Json` を使うと、`overrideReferences: true` では CS0234 になった。対照として `false` にすると、同じコードがコンパイルされて読み込まれた（型が見つかった） |
| 開いているシーンが未保存のとき | **推測**（ツールの説明による）: `tests-run` は例外を出して走らない | 走らせる前に `scenes \| select name isDirty` で確かめる |
| 埋め込みパッケージの中のテスト | **測った**: `manifest.json` に `testables` を足さなくても見つかり、走る | 2026-11-13、`com.obelune.data-viewer` の `Tests/Editor/` に置いたテスト（`overrideReferences: true`・`nunit.framework.dll`）が、`tests \| where fullName starts-with "Obelune.DataViewer"` に 1 件出て、`tests-run` で Passed |
| 新しい埋め込みパッケージを置いたあとの取り込み | **測った**: 1 回目の `RefreshAssets()` ではパッケージは登録された（`package-list` に出た）が、60 秒待っても `.meta` ができず、コンパイルも起きなかった。2 回目の `RefreshAssets()` で取り込まれた。**推測**: 新しいパッケージの登録と、その中のファイルの取り込みが、別々の段階で起きる | 新しいパッケージを置いたときは、`.meta` ができたことを確かめるまで `RefreshAssets()` を呼び直す |
| EditorWindow の見た目を画像で取れるか | **測った**: 取れない。MCP の `screenshot-isolated` は GameObject を描くもの（ツールの説明）。`InternalEditorUtility.ReadScreenPixel` は OS の画面の画素を読むため、Unity が前面にないと**ほかのアプリの画面を写す**（2026-11-13 に、利用者のブラウザを写した。すぐに消した） | **`ReadScreenPixel` など、ホストの画面を読む方法は使わない。**見た目は人が確かめる（下の「実機でしか確かめられないもの」）。エージェントは、ウィンドウの状態（列の数・選択・表示の文字列）をリフレクションで読んで確かめる |
| PlayMode テスト | 測っていない | 当面は使わない。エディタ拡張のテストは EditMode で足りる |

## エージェントが MCP 経由で回すもの（エディタが必要。CI はまだない）

| 対象 | 守れる退行 | 状態 |
|---|---|---|
| 純粋ロジック（整形・解析・比較・パスの判定・直列化の往復など） | 境目の値・空の入力・往復で形が崩れないこと。**黙って壊れる場所** | パッケージができたら、機能ごとに `test-cases.md` に書く |
| Editor API を使う処理のうち、画面を見なくても結果を確かめられるもの（アセットの作成と読み込み・設定の保存と復元・メニューの登録など） | 呼び出しの結果が値として取れるもの | 同上 |
| パッケージがコンパイルできること | asmdef の規則に反する参照・依存の書き忘れ | **テストが通ったことでは確かめられない。** **測った**: コンパイルに失敗している間（CS0234）は接続が切れず、ドメインリロードは起きなかった。**推測**: 前回コンパイルできたアセンブリが残り、古いコードのテストが走る（エラーが出ている間に `tests-run` は呼んでいない）。上の「タグを打つ前の確認」の 3 で防ぐ |

## 実機でしか確かめられないもの（手の往復に残す）

**「なぜ自動化しないか」を必ず書く。**書けないなら、それは自動化できる。

| 対象 | なぜ自動化しないか |
|---|---|
| EditorWindow・Inspector の見た目（ライトスキン・ダークスキン） | 読めるかどうかは、人が見て判断するしかない。MCP で見えている画面写しの道具は `screenshot-isolated` だけで、Game・Scene ビューを写す道具は `Visible: false` |
| 操作の手触り（ドラッグの気持ちよさ・フォーカスの移り方が自然か） | 良し悪しを決めるのは人の感覚。なお、イベントを組み立てて送れば結果を値で確かめられる処理（キーを押したら選択が動く、など）は、`script-execute` やテストで自動化できる。その場合は上の表に入れる |
| 利用側プロジェクトに git URL で入れて動くか | **依存の規則を端から端まで証明できるのはこれだけ。**開発用プロジェクトとは別の、まっさらなプロジェクトが必要になる |
| Unity のバージョン違い（最低対応の 6000.3 以外） | ホストにある Unity は 6000.3.28f1 だけ |

## 判定に使う道具

**製品自身に数字を言わせる**のが基本。推測の代わりに測定を置く。

| 仕組み | 何を分けるためのものか |
|---|---|
| わざと失敗させるテスト（対照） | 「0 件失敗」が**通った**のか**走っていない**のかを分ける。テストの仕組みを変えたときは、必ず一度これで確かめる |
| `tests \| where fullName contains "<名前空間>"`（`rql-query`） | テストが**見つかっていない**（コンパイルできていない・asmdef の設定違い）のか、**見つかったが落ちた**のかを分ける |
| `console \| where logType == "Error"`（`rql-query`） | コンパイルエラーと実行時のエラーを、番号と行で取る |
| `/proc/net/tcp*` の :8080 宛ての ESTABLISHED | エディタとの接続が**切れている**のか、**つながっているが応答がない**のかを分ける。サーバーログには negotiate が記録されないので、ログは代わりにならない |

## 未決

- **CI を作るか**。作るなら、案は 2 つある。どちらにするかは人が決める
  - 純粋ロジックだけを `noEngineReferences: true` の asmdef に分け、コンテナや GitHub Actions で `dotnet test` する。**推測**: Unity のライセンスは要らない。ただし、いまのコンテナには dotnet がない
  - GameCI で Unity をバッチモードで動かし、EditMode テストを走らせる。Unity のライセンスを GitHub の secret に置く必要がある。Unity-MCP のプラグインは CI では、DLL がコミット済みで、`UNITY_MCP_READY` が ProjectSettings に入っている前提で動く（**測った**: プラグインのソース `NuGetDependencyResolver.cs` のコメントと `IsCi()` の分岐）。いまは DLL も印もコミットしているので、この前提を満たしている
