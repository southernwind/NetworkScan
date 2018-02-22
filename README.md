# NetworkScan
ローカルネットワーク内をスキャンして表示します。

![Network Scan](https://raw.githubusercontent.com/southernwind/Images/311c70741f7fd9a8a9262432682b0821aba59731/NetworkScan/main.png)

## 機能と説明

WinPcapが必須です。

ベンダー名を表示したい場合はScan前にVendorListUpdateを行います。(初回のみ)

Scanボタンを押すとネットワークアドレスからブロードキャストアドレスまでのIPアドレスに対して、Request Interval(ms)間隔で指定NICよりARP要求を行います。  
指定したNICにARP応答が返ってくるとIPアドレス、MACアドレスを記録、表示し、ホスト名の逆引きを試みます。  
また、事前にVendorListUpdateをしていた場合、MACアドレスから検索したベンダー名を表示することが出来ます。


## ライセンス
MITライセンス

## 使用ライブラリ
* [CsvHelper](https://github.com/JoshClose/CsvHelper)  
Copyright JoshClose  
MS-PL

* [Livet](https://github.com/ugaya40/Livet)   
Livet Copyright (c) 2010-2011 Livet Project   
zlib/libpng

* [MahApps.Metro](https://github.com/MahApps/MahApps.Metro/blob/master/LICENSE)  
[MIT License](https://github.com/southernwind/NetworkScan/licenses/MahApps.Metro.txt)  

* [MaterialDesignInXamlToolkit](https://github.com/ButchersBoy/MaterialDesignInXamlToolkit)  
[MIT License](https://github.com/southernwind/NetworkScan/licenses/MaterialDesignInXamlToolkit.txt)  

* [PcapDotNet](https://github.com/PcapDotNet/Pcap.Net)  
Copyright (c) 2009, Boaz Brickner  
BSD-3-Clause

* [ReactiveProperty](https://github.com/runceel/ReactiveProperty)  
[MIT License](https://github.com/southernwind/NetworkScan/licenses/ReactiveProperty.txt)