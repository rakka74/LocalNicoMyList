【  ソフト名  】SharpHeaderCookie
【   作成者   】うつろ@悠悠閑閑前途遼遠
【    概要    】ウェブブラウザのクッキーを取得・共有するライブラリ
【  動作環境  】.NET Framework {4.5.1, 4.5.2, 4.6, 4.6.1, 4.6.2} windows {7sp1, 8.1update, windows10}
【ソースモデル】プロプライエタリ
【 ライセンス 】独自ライセンス下記参照


○ライセンス

・ライセンスはCC BY-ND 4.0にしたいけれどソフトウェアへの適用は非推奨のようですね。独自ライセンスを採用します。
 1．非商用、商用例外なくこのライブラリを複製・再配布できます。
 2．このライブラリを改変・改造した場合、再配布できません。
 3．readmeや情報成果物の目立たないどこかに
SharpHeaderCookie (c) 2015-2016 悠悠閑閑前途遼遠, All Rights Reserved.
と表示してください。
・ご利用の報告義務はありませんので上記3点をお守りいただきご自由にご利用ください。
・強制ではありませんが http://www.youyoukankan.net./cms/SharpHeaderCookie.html にリンクまたは表示していただけたらうれしいです！


○対応ウェブブラウザ

・Internet Explorer
・Internet Explorer Protected Mode
・Internet Explorer Enhanced Protected Mode
・Edge (Project Spartan)
・Mozilla Firefox
・SeaMonkey
・Pale Moon
・Comodo IceDoragon
・Cyberfox
・Nightingale
・Chromium
・Google Chrome
・Opera
・Vivaldi
・Super Bird
・Kinza
・Tungsten
・Comodo Dragon
・Sleipnir
・Chromodo
・MaxthonNitro
・Slimjet
・Iridium
・CocCoc
・CentBrowser
・YandexBrowser


○IEPCookie.dll

IEの（拡張）保護モードのCookieHeader取得用


○注意事項

・このライブラリを使用したことによって生じたすべての損害について責任はもちません。
・無保証です。
・このライブラリを使用し電子計算機に過度な負荷を与えるような情報成果物の作成を禁止します。
・ソースモデルをプロプライエタリとしましたのでILSpy等を使用して逆コンパイルすることは構いませんが得られたソースそのものを転載または掲載することを禁止します。
（見たってひどいソースなんですからみないほーがいいよっ）
・ソースモデルがプロプライエタリですのでソース公開を必須とするライセンスの情報成果物とリンクできません。


○サードパーティーライブラリー
System.Data.SQLite.dll=1.0.103.0
https://www.sqlite.org/
http://system.data.sqlite.org/index.html/doc/trunk/www/downloads.wiki



○更新履歴
2016-11-03 1.0.8.0
windows 10 1607 Edge 対応。
CoolNovo 削除。
CentBrowser 追加。
YandexBrowser 追加。
System.Data.SQLite.dll=1.0.103.0

2015-09-07 1.0.7.0
IEPCookie.dllのフォルダ走査仕様を変更。

2015-07-19 1.0.6.0
Slimjet、Iridium、CocCoc追加。

2015-07-05 1.0.5.0
windows10 Build10162のEdge追加。

2015-07-03 1.0.4.0
Sleipnir、Chromodo、MaxthonNitro追加。
Blink取得不具合を修正。
（デフォルト位置のCookieファイル検出の強化）

2015-06-20 1.0.3.0
Blink取得不具合を修正。
（平文、暗号化混合データのCookieファイルのとき取得が失敗する不具合を修正）

2015-06-11 1.0.2.0
System.Data.SQLite.dllを1.0.97.0に更新。
Edge(Project Spartan)に暫定対応。

2015-04-25 1.0.1.0
SharpHeaderCookie.IGetBrowserCookie.CookieHeader API の拡張
（クッキー名を複数指定できたり、キャッシュを使うか使わないか指定できたり）
SharpHeaderCookie.IGetBrowserCookie.Cookies API の追加。
（どうしてか最初からObsolete属性付）

2015-04-01 1.0.0.8
NuGet対応
x86フォルダ内のIEPCookie.dllのプロパティの出力ディレクトリにコピーの値を新しい場合はコピーするに設定が必要です。NuGetの仕様が理解できたら自動コピーするように修正します。

2015-03-30 1.0.0.7
初公開



-- 
SharpHeaderCookie (c) 2015-2016 悠悠閑閑前途遼遠, All Rights Reserved.
http://www.youyoukankan.net./cms/SharpHeaderCookie.html
