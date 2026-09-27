Add-Type -AssemblyName System.Windows.Forms,System.Drawing
$toolRoot = Split-Path $PSScriptRoot -Parent
$artifacts = Join-Path $PSScriptRoot 'artifacts'
if (-not (Test-Path -LiteralPath $artifacts)) { New-Item -ItemType Directory -Path $artifacts | Out-Null }
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $toolRoot 'dist\OmenUndervolt.exe'))
[Windows.Forms.Application]::EnableVisualStyles()
$formType = $assembly.GetType('OmenUndervolt.MainForm')
$form = [Activator]::CreateInstance($formType, $true)
$form.PerformLayout()
$root = $form.Controls[0]
$root.Font = $form.Font
$root.BackColor = $form.BackColor
$root.ForeColor = $form.ForeColor
$form.Controls.Remove($root)
$root.Dock = [Windows.Forms.DockStyle]::None
$root.Size = $form.ClientSize
$root.CreateControl()
$root.PerformLayout()
$bitmap = New-Object Drawing.Bitmap($root.Width, $root.Height)
$root.DrawToBitmap($bitmap, $root.ClientRectangle)
$bitmap.Save((Join-Path $artifacts 'ui-preview.png'))
function Read-Layout($control, $depth) {
    foreach ($child in $control.Controls) {
        if ($child -is [Windows.Forms.Panel] -or $child -is [Windows.Forms.Button] -or $child -is [Windows.Forms.Label] -or $child -is [Windows.Forms.PictureBox]) {
            Write-Output (('{0} {1}: {2}' -f $child.GetType().Name,$child.Text,$child.Bounds))
        }
        Read-Layout $child ($depth+1)
    }
}
Read-Layout $root 0
$bitmap.Dispose()
$form.Dispose()


$dialogType = $assembly.GetType('OmenUndervolt.RestartChoiceDialog')
$dialog = [Activator]::CreateInstance($dialogType,[Reflection.BindingFlags]'Instance,NonPublic',$null,@('-130 mV'),$null)
$dialog.PerformLayout()
$panel = $dialog.Controls[0]
$panel.Font = $dialog.Font
$panel.BackColor = $dialog.BackColor
$dialog.Controls.Remove($panel)
$panel.Dock = [Windows.Forms.DockStyle]::None
$panel.CreateControl()
$panel.Size = $panel.GetPreferredSize([Drawing.Size]::new(440,0))
$panel.PerformLayout()
$preview = New-Object Drawing.Bitmap($panel.Width,$panel.Height)
$panel.DrawToBitmap($preview,$panel.ClientRectangle)
$preview.Save((Join-Path $artifacts 'restart-preview.png'))
$preview.Dispose()
$panel.Dispose()
$dialog.Dispose()

