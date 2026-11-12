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

## 全体アーキテクチャ

### 作業環境

```
メインマシン（Windows）
 ├── Unity エディタ ……… UnityProject/ を開いて動作確認する
 └── Dev Container（Linux）
       └── エージェント … ドキュメントとコードを書く（Unity は動かせない）

  ※ 作業ツリーは両者で共有する（改行は .gitattributes で LF に統一）
```

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
- 開発用プロジェクトには、IDE 連携・Test Framework・ビルトインモジュール以外のパッケージを入れない。ほかのパッケージが入っていると、パッケージ側に依存を書き忘れても開発用プロジェクトではコンパイルが通ってしまい、利用側プロジェクトに入れたときに初めて壊れる。パッケージに依存が必要なときは、そのパッケージの `package.json` に書く
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
