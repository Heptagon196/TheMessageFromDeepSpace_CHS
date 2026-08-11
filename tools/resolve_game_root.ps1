function Resolve-GameRootPath {
    param(
        [AllowEmptyString()]
        [string]$GameRoot = "",

        [Parameter(Mandatory = $true)]
        [string]$ProjectRoot
    )

    $projectRootFullPath = [IO.Path]::GetFullPath($ProjectRoot)
    if (-not [string]::IsNullOrWhiteSpace($GameRoot)) {
        if ([IO.Path]::IsPathRooted($GameRoot)) {
            $candidate = $GameRoot
        }
        else {
            $candidate = Join-Path (Get-Location).Path $GameRoot
        }
    }
    else {
        $configPath = @(
            (Join-Path $projectRootFullPath 'local.config.json'),
            (Join-Path $projectRootFullPath 'project.config.json')
        ) | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1

        if ([string]::IsNullOrWhiteSpace($configPath)) {
            $candidate = Split-Path -Parent $projectRootFullPath
        }
        else {
            try {
                $config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
            }
            catch {
                throw "无法读取配置文件 $configPath：$($_.Exception.Message)"
            }
            $configuredGameRoot = [string]$config.GameRoot
            if ([string]::IsNullOrWhiteSpace($configuredGameRoot)) {
                throw "配置文件缺少非空的 GameRoot：$configPath"
            }
            if ([IO.Path]::IsPathRooted($configuredGameRoot)) {
                $candidate = $configuredGameRoot
            }
            else {
                $candidate = Join-Path (Split-Path -Parent $configPath) $configuredGameRoot
            }
        }
    }

    $fullPath = [IO.Path]::GetFullPath($candidate)
    if (-not (Test-Path -LiteralPath $fullPath -PathType Container)) {
        throw "游戏根目录不存在：$fullPath"
    }

    return (Resolve-Path -LiteralPath $fullPath).Path
}

function Resolve-GameManagedDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$GameRoot
    )

    $managedDirectory = Join-Path $GameRoot "The Message From Deep Space_Data\Managed"
    if (-not (Test-Path -LiteralPath $managedDirectory -PathType Container)) {
        throw "游戏程序集目录不存在：$managedDirectory"
    }

    return (Resolve-Path -LiteralPath $managedDirectory).Path
}
