# LocalNicoMyList
ニコニコ動画のマイリストをローカルで管理するアプリケーション。

* フォルダ一覧の右クリックメニューからフォルダの追加、削除、名前の変更ができます。
* フォルダ一覧のフォルダをドラッグ＆ドロップすることで並び順を変更できます。
* マイリスト一覧に動画のURL(`http://www.nicovideo.jp/watch/sm～`)をドラッグすると選択されているフォルダにマイリストが追加されます。
* マイリスト一覧にマイリストのURL(`http://www.nicovideo.jp/mylist/～`)をドラッグするとニコニコ動画のマイリストの動画がまとめてマイリストに追加されます。
* 動画をダブルクリックでブラウザで動画のページを開きます。
* 更新ボタンで再生数などの情報を最新の値に更新します。
* マイリスト登録時には最新コメント日時の取得が行われません。時間がたつとgetflvの項目が○になるので、その後、更新ボタンを押すと最新コメント日時が取得されるようになります。

### スクリーンショット
![タイトル](screenshot.png)

### 使用ライブラリ
* DynamicJson (c) neuecc
* ListViewDragDropManager (c) Josh Smith, Anthony Perez
* SharpHeaderCookie (c) 悠悠閑閑前途遼遠
* SQLite (c) SQLite Development Team
