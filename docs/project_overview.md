# プロジェクト概要

## 目的・背景

Unity エディタでの作業を効率化する拡張機能を、継続的に作り足していくためのリポジトリ。
作った拡張機能は個別の UPM パッケージとして配布し、他の Unity プロジェクトから必要なものだけを取り込めるようにする。

## スコープ

### 作るもの

- Unity エディタ拡張（EditorWindow・Inspector 拡張・メニュー・ツール類など）。個々の機能は `docs/index.md` の機能エリアとして追加する
- 機能ごとに独立した UPM パッケージ（`com.obelune.<機能名>`）
- パッケージを開発・動作確認するための Unity プロジェクト（`UnityProject/`）

### 作らないもの

- ランタイム（ビルドされたゲームに含まれる）機能。必要になったらその機能エリアで改めて決める
- Asset Store での配布
- パッケージレジストリ（OpenUPM 等）への公開。当面は git URL での配布のみ

## 技術スタック概要

| 技術 | 用途 | 選定理由 |
|------|------|----------|
| Unity 6.3 LTS（6000.3） | 開発・動作確認用のエディタ、パッケージの最低対応バージョン | 2026-11 時点の最新 LTS（サポートは 2027-12 まで）。特定バージョンへのこだわりはない |
| C# / Unity Editor API | 拡張機能の実装 | Unity エディタ拡張の標準 |
| UPM（Unity Package Manager）パッケージ | 配布単位 | git URL で必要な機能だけ導入できる |
| Assembly Definition（asmdef） | パッケージごとのアセンブリ分離 | パッケージ間の依存を明示し、Editor 専用コードをビルドから除外する |
| Dev Container（Debian + Claude Code） | エージェントの作業環境 | ホスト環境を汚さずにエージェントを動かす |
| Unity-MCP（`com.ivanmurzak.unity.mcp` 1.11.1 + GameDev-MCP-Server 9.5.1） | コンテナ内のエージェントからエディタを操作する（コンソール・テスト実行・シーン操作など） | Unity 側から接続しに来る方式なので、ポート転送だけでコンテナの壁を越えられる。Unity 公式 CLI はエディタが localhost 接続しか受け付けず、コンテナからは届かない |

## 全体アーキテクチャ

### 作業環境

```
メインマシン（Windows）
 ├── Unity エディタ ……… UnityProject/ を開いて動作確認する
 │     └── Unity-MCP プラグイン ──→ localhost:8080 ─┐ VS Code のポート転送
 └── Dev Container（Linux, /workspace）              │
       ├── gamedev-mcp-server（:8080） ←───────────┘
       │     ↑ stdio（Claude Code が起動する。.mcp.json）
       └── エージェント … ドキュメントとコードを書き、MCP 経由でエディタを操作する

  ※ 作業ツリーは両者で共有する（改行は .gitattributes で LF に統一）
```

Unity-MCP の接続条件:

- サーバーのバイナリは `.devcontainer/setup.sh` がコンテナ作成時に取得する（`.unity-mcp/`、Git 管理外）。サーバーのバージョンは、プラグインが想定するバージョン（`McpServerManager.ServerVersion`）と一致させる。プラグインを上げるときは `setup.sh` の版と SHA256 も更新する
- Unity 側の `Window > AI Game Developer` は、接続モード **Custom**・接続先 `http://localhost:8080`・通信方式 **stdio** にする。stdio 以外にすると、プラグインが Windows 側でも自前のサーバーを起動してしまう
- Claude Code はセッションごとにサーバーを起動するので、エディタを操作できるセッションは同時に 1 つだけ。Claude Code を起動してから Unity 側で Connect する

Unity-MCP の接続状況（2026-11-12 時点）:

| 項目 | 状態 | 根拠 |
|------|------|------|
| Claude Code がサーバーを起動し、MCP の初期化が通る | **測った**: 通る | `server-log.txt` に `Client (claude-code 2.1.322) method 'initialize' ... completed` |
| サーバーの待ち受けアドレス | **測った**: `127.0.0.1:8080` と `[::1]:8080` だけ | `/proc/net/tcp*` の LISTEN 行。ログにも `bind: loopback`。コンテナの IP（`172.17.0.2:8080`）への接続は拒否される |
| コンテナ内から hub に届くか | **測った**: 届く | `curl -X POST http://localhost:8080/hub/mcp-server/negotiate` → HTTP 200 |
| Unity プラグインからの接続 | **測った**: 来ていない | ログに `No engine plugin is connected`。`/proc/net/tcp` に :8080 宛ての ESTABLISHED がない |
| プラグイン未接続のときのエージェント側 | **測った**: ai-game-developer のツールはセッションに 1 つも出てこない | サーバーは `Returning server-native tools only` と返したが、Claude Code のツール一覧には何も出ない |
| VS Code のポート転送で、`127.0.0.1` で待ち受けるサーバーに届くか | **推測**（未確定）: 届くはず。VS Code の転送はコンテナ内から localhost に接続するため | Unity 側で Connect したときに、`/proc/net/tcp` に :8080 宛ての ESTABLISHED が出るか、ログに接続の記録が出るかで決まる。この記録の時点では Unity からの接続が来ておらず、どちらとも言えない |
| プラグインが後から接続したとき、起動済みのセッションにツールが出るか | **推測**（未確定） | Unity で Connect したあと、同じセッションでツール一覧を確かめれば決まる |
| `Assets/Plugins/NuGet/` に入る依存 DLL（45 個、18 MB） | **測った**: プラグインの導入時に作られ、すべて `isExplicitlyReferenced: 0`（全アセンブリから自動で参照される） | 各 `.meta`。上の「制約」にある、パッケージの依存の書き忘れを見逃す状態になる。コミットするか、扱いをどうするかは未決 |

### リポジトリ内の配置

```
UnityProject/                          # 開発用 Unity プロジェクト
├── Assets/                            # 動作確認用のシーン・素材のみ。配布物は置かない
├── Packages/
│   ├── manifest.json
│   └── com.obelune.<機能名>/            # 埋め込みパッケージ = 配布単位（機能ごとに 1 つ）
│       ├── package.json
│       ├── README.md
│       ├── CHANGELOG.md
│       ├── Editor/
│       │   └── Obelune.<機能名>.Editor.asmdef
│       └── Tests/
│           └── Editor/
│               └── Obelune.<機能名>.Editor.Tests.asmdef
└── ProjectSettings/
```

- パッケージは `UnityProject/Packages/` に**埋め込みパッケージ**として置く。Unity で開いたまま編集でき、そのまま配布物になる
- 名前の規則: パッケージ名は `com.obelune.<機能名>`（小文字ケバブケース）、名前空間とアセンブリ名は `Obelune.<機能名>`（PascalCase）
- パッケージ間に依存がある場合は `package.json` の `dependencies` に書く

### 配布方法

利用側プロジェクトの Package Manager で「Add package from git URL」に次の URL を入力する。

```
https://github.com/kairamu061-dev/UnityEditorExtension.git?path=UnityProject/Packages/com.obelune.<機能名>
```

バージョンを固定する場合は、URL の末尾に `#<タグ>` を付ける。タグは `com.obelune.<機能名>/<バージョン>` の形式とする。

## 制約

- エージェントの作業環境（Dev Container）には Unity がない。コンパイルと動作の確認はメインマシンの Unity エディタで行う（`rules/verification.md`）
- `.meta` ファイルは Unity が作る。エージェントが新しいファイルを作ったら、Unity で一度読み込んで `.meta` ができてからコミットする。`.meta` が欠けたままのパッケージは、利用側でアセットの参照が壊れる
- 開発用プロジェクトには、IDE 連携・Test Framework・ビルトインモジュール・開発用ツール（Unity-MCP）以外のパッケージを入れない。開発用ツールにはどの配布パッケージも依存しない。ほかのパッケージが入っていると、パッケージ側に依存を書き忘れても開発用プロジェクトではコンパイルが通ってしまい、利用側プロジェクトに入れたときに初めて壊れる。パッケージに依存が必要なときは、そのパッケージの `package.json` に書く
- 利用者が他プロジェクトに入れても問題が出ないよう、各パッケージは `com.obelune.*` 以外の外部パッケージに依存しないことを原則とする

## UI デザイン方針

### コンセプト

Unity 標準のエディタ UI に溶け込む見た目にする。独自の装飾は加えず、ユーザーがすでに知っている操作感を保つ。

### カラーパレット

独自のカラーパレットは定義しない。`EditorStyles`、USS の変数、`EditorGUIUtility.isProSkin` を使い、ライトスキンとダークスキンの両方で読めるようにする。

### タイポグラフィ

Unity エディタ標準のフォントとサイズに従う。

### コンポーネント方針

- 新しく作るウィンドウは UI Toolkit を優先する。既存の IMGUI の画面（Inspector の一部など）を拡張する場合は IMGUI を使う
- メニューは `Tools/Obelune/<機能名>` の下にまとめる
