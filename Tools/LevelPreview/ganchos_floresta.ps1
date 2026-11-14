# Auditoria dos ganchos da Floresta (pedido de 14/11/2026): cada gancho alcançável com o Caçador, na borda de um chão
# onde o gato sobe pendurado nele, e se leva a um lugar que sem o Caçador não se alcança ([lugar novo]).
# Uso (na raiz do projeto):  powershell -ExecutionPolicy Bypass -File Tools/LevelPreview/ganchos_floresta.ps1
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$ed = Join-Path (Resolve-Path (Join-Path $here '..\..')).Path 'Assets\_Game\Editor'
Add-Type -Path @((Join-Path $ed 'ForestLayout.cs'), (Join-Path $ed 'ForestReach.cs'))
[SeteVidas.EditorTools.ForestReach]::HookAudit()
