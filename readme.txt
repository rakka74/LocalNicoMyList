LocalNicoMyList 1.0 (c) 2016 rakka
https://github.com/rakka74/LocalNicoMyList

■ 概要
ニコニコ動画のマイリストをローカルで管理するアプリケーション。

■ 動作確認環境
Windows 7 Home Premium SP1

■ 使い方
● フォルダ一覧
・右クリックメニューからフォルダの追加、削除、名前の変更ができます。
・フォルダをドラッグ＆ドロップすることで並び順を変更できます。
● マイリスト一覧
・動画のURL(`http://www.nicovideo.jp/watch/sm～`)をドラッグ＆ドロップ
すると選択されているフォルダにマイリストが追加されます。
・マイリスト一覧にマイリストのURL(`http://www.nicovideo.jp/mylist/～`)を
ドラッグ＆ドロップするとニコニコ動画のマイリストの動画がまとめて
マイリストに追加されます。
・マイリストをダブルクリックでブラウザで動画のページを開きます。
・マイリストを選択して右クリックメニューからマイリストを削除できます。
・更新ボタンで再生数などの情報を最新の値に更新します。
・マイリスト登録時には最新コメント日時の取得が行われません。時間がたつと
getflvの項目が○になるので、その後、更新ボタンを押すと最新コメント日時が
取得されるようになります。

■ 開発環境
Windows 7 Home Premium SP1
Microsoft Visual Studio Community 2015

■ 使用ライブラリ
・DynamicJson (c) neuecc
・ListViewDragDropManager (c) Josh Smith, Anthony Perez
・SharpHeaderCookie (c) 悠悠閑閑前途遼遠
・SQLite (c) SQLite Development Team

