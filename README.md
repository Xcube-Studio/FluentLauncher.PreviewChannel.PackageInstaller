# FluentLauncher.PreviewChannel.PackageInstaller

> [!IMPORTANT] 
> _**此工具已弃用，请转到 [FluentLauncher.Preview.Installer](https://github.com/Xcube-Studio/FluentLauncher.Preview.Installer)**_  

## ❓这是什么

这是 Fluent Launcher 预览通道更新包安装器，从 Release 中下载 `PackageInstaller.exe` 将其与 `updatePackage.zip` 放在同一目录下即可更新  
该程序使用 .NET 9 AOT 编译，运行不需要安装 .NET 运行时  
该程序安装更新包时需要管理员权限，若未以管理员权限运行，会运行时请求管理员权限

> 程序最初设计是作为 Fluent Launcher 预览通道自更新脚本  
> 启动器负责下载更新包与本安装程序，安装程序负责执行更新  
> 无论是 `PackageInstaller.exe` 还是 `updatePackage.zip` 都只适用于预览通道  
> 我们不发布 正式版（Stable）、开发版（Dev）的更新包，启动器自更新也只有预览通道才有

## 📜详细命令
该程序使用 `System.CommandLine` 包，下面列出了该程序支持的命令，[命令语法请参考此处](https://learn.microsoft.com/zh-cn/dotnet/standard/commandline/syntax)

### 根命令 (即双击运行)
查找当前目录下适用的平台架构的更新包 (例 `updatePackage-x64.zip`) 进行更新安装，可选是否在安装成功时启动应用

| 选项                    | 类型 | 默认值 | 是否必须 | 描述              |
|------------------------|------|-------|----------|-------------------|
| --launchAfterInstalled | bool | True  | 否       | 安装成功后启动应用  |

### manualTargetPackage 命令
手动指定安装的 msix 包及其依赖 msix，同时可选指定安装包的证书（`若不填则默认使用内置证书，但证书签名不一样会导致无法成功安装`）

| 选项                     | 类型      | 默认值 | 是否必须 | 描述                |
|--------------------------|----------|-------|----------|---------------------|
| --launchAfterInstalled   | bool     | True  | 否       | 安装成功后启动应用    |
| --packagePath            | string   | N/A   | 是       | 安装的 msix 包路径   |
| --dependencyPackagesPath | string[] | N/A   | 是       | 依赖的 msix 包路径   |
| --certificationPath      | string?  | null  | 否       | 证书 (.cer) 文件路径 |

### query 命令
使用 api.github.com 查询预览通道相关信息，目前仅可查询某个版本的预览构建次数

| 选项                     | 类型      | 默认值 | 是否必须 | 描述                  |
|--------------------------|----------|-------|----------|----------------------|
| --getBuildCountOfVersion | string   | null  | 否       | 查询预览构建次数的版本 |

### generateReleaseJson 命令
根据给定的信息输出预览通道构建信息 Json

例如 ` generateReleaseJson --updatePackageFiles "updatePackage-x64.zip" "updatePackage-arm64.zip" --stableVersion 2.3.2.0 --commit d088404 ` 输出如下  

``` json
{
  "commit": "d088404",
  "build": 4,
  "releaseTime": "12/28/2024 04:50:55",
  "currentPreviewVersion": "2.3.2.4",
  "previousStableVersion": "2.3.2.0",
  "hashes": {
    "updatePackage-x64.zip": "d70b78488d76701bfae7dc7ce660585c",
    "updatePackage-arm64.zip": "eb35d74daeb69c2e4a9f882b8b8abf63"
  }
}
```

| 选项                 | 类型      | 默认值 | 是否必须 | 描述                          |
|----------------------|----------|-------|----------|-------------------------------|
| --stableVersion      | string   | N/A   | 是       | 用于查找预览构建次数的稳定版本号 |
| --commit             | string   | N/A   | 是       | commit 提交号                  |
| --updatePackageFiles | string[] | N/A   | 是       | 用于计算 hash 值得更新包路径    |

### generateReleaseMarkdown 命令
与 `generateReleaseMarkdown` 命令类似，但该命令会将其写入文件

例如 ` generateReleaseMarkdown --markdownPath "body.md" --updatePackageFiles "updatePackage-x64.zip" "updatePackage-arm64.zip" --stableVersion 2.3.2.0 --commit d088404 ` 会将以下内容写入 `body.md`  

````
``` json
{
  "commit": "d088404",
  "build": 4,
  "releaseTime": "12/28/2024 04:50:55",
  "currentPreviewVersion": "2.3.2.4",
  "previousStableVersion": "2.3.2.0",
  "hashes": {
    "updatePackage-x64.zip": "d70b78488d76701bfae7dc7ce660585c",
    "updatePackage-arm64.zip": "eb35d74daeb69c2e4a9f882b8b8abf63"
  }
}
```
````

| 选项                 | 类型      | 默认值 | 是否必须 | 描述                          |
|----------------------|----------|-------|----------|-------------------------------|
| --stableVersion      | string   | N/A   | 是       | 用于查找预览构建次数的稳定版本号 |
| --commit             | string   | N/A   | 是       | commit 提交号                  |
| --updatePackageFiles | string[] | N/A   | 是       | 用于计算 hash 值得更新包路径    |
| --markdownPath       | string   | N/A   | 是       | 写入的 markdown 文件地址       |
