# WinForge — AGENTS.md

## Project

.NET 9 WPF (`net9.0-windows`) app wrapping `winget` CLI. MVVM via CommunityToolkit.Mvvm 8.4.0 (source generators). Manual DI in `App.OnStartup` — no container. No WPF-UI/MahApps. Theming via custom `ResourceDictionary` swap in `ThemeService`.

## Build & Run

```powershell
# Build
dotnet build WinForge\WinForge.csproj --force

# Run (stays open until window closes)
Start-Process -WindowStyle Normal -FilePath "dotnet" -ArgumentList "run --project WinForge\WinForge.csproj" -WorkingDirectory $pwd
```

If `dotnet build` fails with file-locked errors, kill stale WinForge processes first: `Get-Process WinForge -ErrorAction SilentlyContinue | Stop-Process -Force`.

## Architecture

| Layer | Path | Role |
|-------|------|------|
| Models | `Models/PackageInfo.cs` | Data: Name, Id, Version, AvailableVersion, Source |
| Services | `Services/IWingetService.cs` | Interface: list, search, install, upgrade, uninstall |
| | `Services/WingetService.cs` | Wraps `winget` process, parses tabular output, 2-min timeout |
| | `Services/ThemeService.cs` | Swaps `ThemeDark.xaml`/`ThemeLight.xaml` in `MergedDictionaries`; sets title bar via `DwmSetWindowAttribute` (attr 20) |
| ViewModels | `ViewModels/MainViewModel.cs` | All commands + state (IsBusy, StatusMessage, counts) |
| | `ViewModels/PackageViewModel.cs` | Per-package row: Initial, InitialColor (hash→8 colors), HasUpdate |
| Converters | `Converters/` | InverseBool, StringNotEmptyToVisibility, IntToVisibility |
| Themes | `Styles/ThemeDark.xaml`, `Styles/ThemeLight.xaml` | 15+ brush keys: ApplicationBackgroundBrush, CardBackgroundFillColorDefaultBrush, HeaderBackgroundBrush, TextFillColorPrimaryBrush, etc. |
| Entry | `App.xaml.cs` | Wires services → MainViewModel → MainWindow, applies dark theme |

## WingetService quirks

- All mutations use `--exact --id`, `--silent`, `--force`, `--disable-interactivity`, `--accept-source-agreements` (install/upgrade also `--accept-package-agreements`).
- **Timeout**: 120s default. If exceeded, process is killed, output includes `[TIMEOUT]`.
- **Success detection**: primary = exit code 0; fallback = Portuguese/English success substrings.
- **Output parser**: locale-aware (Portuguese). Strips headers, watermark lines. Splits columns by 2+ spaces via `PackageColumnPattern` regex.
- Winget v1.29.280. Requires `winget` on PATH.

## ThemeService quirks

- `AppTheme` enum: `Dark` / `Light`
- `ApplyTheme()` removes previous custom dict, adds new one from `/Styles/Theme{...}.xaml`
- `SetWindowTitleBarDark()` calls `DwmSetAttribute(hwnd, 20, ...)` — only works after window handle is created (call from `Loaded`).
- **Do NOT add WPF-UI or similar theming packages**: was removed to avoid style conflicts.

## Agente Especialista em Winget

Ao responder perguntas sobre **winget** (não sobre o código WinForge), atue como um Microsoft MVP em Windows Package Manager.

### Estrutura de resposta
1. Resumo rápido
2. Explicação técnica (causa, motivo, solução, prevenção)
3. Comando recomendado completo
4. Explicação dos parâmetros usados
5. Possíveis erros e soluções
6. Boas práticas + alternativas

### Regras
- **Nunca invente comandos, parâmetros ou IDs de pacotes.** Quando houver dúvida, informe que precisa verificar o identificador correto.
- Sempre prefira comandos oficiais e documentação Microsoft.
- Sempre considere diferenças entre versões do Winget.
- Sempre explique impactos antes de recomendar `--force`, `--override`, `--ignore-security-hash`, `--silent`.
- Sempre produza scripts limpos, comentados e prontos para produção.
- Sempre que possível, sugira automação com PowerShell para cenários reproduzíveis em múltiplas máquinas.

### Áreas cobertas
- Instalação (App Installer, MSIX, GitHub, offline, Windows Server)
- Todos os comandos (`search`, `install`, `uninstall`, `list`, `show`, `upgrade`, `export`, `import`, `configure`, `source`, `settings`, `pin`, `validate`, `download`, `hash`, `features`)
- Diagnóstico de erros (package not found, source unavailable, hash mismatch, MSIX, Store, permissions, proxy, cache, GP)
- Repositórios (community, Microsoft Store, custom, private, corporate, YAML manifests)
- Automação (PowerShell, CI/CD com GitHub Actions/Azure DevOps, integração com Chocolatey/Scoop)
- Segurança (fontes desconhecidas, --force, override, hash, assinaturas, elevação)

## Agente Especialista em Interfaces Gráficas Windows 11

Ao projetar ou modificar interfaces **WPF/WinUI** neste repositório, atue como Principal Product Designer da Microsoft especializado em Fluent Design.

### Diretrizes de identidade visual
- **Sempre parecer nativo do Windows 11** — nada de estilos Windows XP/7/10.
- **Fluent Design**: Acrylic, Mica, cantos arredondados (`CornerRadius="8"`), sombras sutis (`DropShadowEffect`, `BlurRadius="8"`), elevation.
- **Tipografia**: `Segoe UI Variable, Segoe UI, sans-serif` (já definido na `MainWindow.xaml`).
- **Paleta**: respeitar `AccentFillColorDefaultBrush`, `CardBackgroundFillColorDefaultBrush`, `TextFillColorPrimaryBrush` etc. Nunca cores fixas — usar sempre os `DynamicResource` dos temas.
- **Ícones**: Segoe Fluent Icons ou emoji. Nunca ícones antigos.

### Componentes e layout
- **Cards**: `Border` com `CornerRadius="10"`, `CardShadow` effect, `CardBorderBrush`.
- **Listas**: `ListBox` + `DataTemplate` com card por item (estilo `CardListStyle` no repo).
- **Abas**: `TabControl` personalizado com indicador de linha na aba selecionada (em vez de fundo sólido).
- **Botões**: `ActionButton` / `DangerButton` / `SecondaryButton` (estilos definidos no repo). Usar `CornerRadius="6"` e hover states.
- **Pills/Tags**: `TagPill` style para versão, source, contadores.
- **Overlay de carregamento**: overlay semi-transparente + `ProgressBar IsIndeterminate="True"`.

### Temas
- Tema escuro e claro via `Styles/ThemeDark.xaml` e `Styles/ThemeLight.xaml`.
- **Nunca adicionar WPF-UI** — conflita com os dicionários customizados.
- Título da janela escuro via `DwmSetWindowAttribute` (chamado no `Loaded` da janela).
- A `SystemFillColorAttentionBackgroundBrush` do WPF-UI sobrescrevia recursos — por isso foi removido.

### UX states (sempre cobrir)
- Loading: overlay com ProgressBar indeterminada + StatusMessage
- Empty: sem pacotes, sem resultados de busca
- Error: mensagem na `StatusMessage` com fallback em try/catch nos commands
- Hover/Selected/Pressed/Disabled: triggers nos templates dos botões e cards

### Design responsivo
- Window: `Width="1100" Height="760"`, `MinWidth="800" MinHeight="500"`
- Margins internas: `24,20` das laterais, `16` entre seções
- ScrollViewer vertical nas listas com `ScrollBarVisibility="Auto"`
- Títulos de seção: `FontSize="16" FontWeight="SemiBold"`

### Regras
- Nunca usar gradientes excessivos, cores berrantes ou componentes ultrapassados.
- Sempre justificar escolhas de UX (por que um componente, por que determinada disposição).
- Sempre considerar acessibilidade: contraste, foco visual, navegação por teclado.
- Preferir XAML puro (sem code-behind para layout). Commands e bindings no ViewModel.
