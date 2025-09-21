# CSharpUpdate
C#写的软件在线OTA升级Demo



本代码基于Visual Studio 2022和dotnet4.8编写的Winform程序，双击`UpdateTest.sln`即可运行。

可能需要手动获取`Nuget`包，因为使用了`ICSharpCode.SharpZipLib.dll`这个解压缩插件。

此软件通过与服务器的版本号比较，安装包也存放于服务器，这里的服务器使用的是`Github和Cloudflare`的Pages服务，个人使用免费。

大概思路如下：

```mermaid
flowchart TD
    A([主程序])
    Q([Github和Cloudflare的pages托管文件])
    
    A --> B[软件启动时静默检查更新]
    A --> C[手动检查更新]
    
    B --> D{存在新版本?}
    D -- 比对文件是 --> E[弹窗提示更新]
    D -- 比对文件否 --> F[静默: 已是最新版本]
    F --> G[退出更新]
    
    C --> H{存在新版本?}
    H -- 比对文件是 --> E
    H -- 比对文件否 --> I[弹窗提示: 已是最新版本]
    I --> G
    
    E --> J[启动升级程序]
    J --> K([升级程序])
    K --> L[下载最新程序]
    L --> M[解压缩<br>CSharpZipLib]
    M --> N[替换文件]
    N --> O[创建批处理文件]
    O --> P([升级成功])
    P --> G[退出更新]
    
    Q([Github和Cloudflare的pages托管文件]) --> R[比对文件]
    Q([Github和Cloudflare的pages托管文件]) --> S[更新包]
    S[更新包] --> L[下载最新程序]
```



更新：

1、支持解压密码；

2、当检测到远端的版本号为0.0.0则删除所有文件。

