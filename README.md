<div align="center">
  <img src="WinForge/icon.ico" alt="WinForge" width="80" height="80" />
  <h1>WinForge</h1>
  <p><strong>Gerenciador de Pacotes winget com Interface Gráfica Moderna</strong></p>
  <p>
    <img src="https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet" alt=".NET 9" />
    <img src="https://img.shields.io/badge/WPF-net9.0--windows-512BD4?logo=windows" alt="WPF" />
    <img src="https://img.shields.io/badge/licença-MIT-green" alt="License" />
  </p>
  <br/>
  <a href="https://github.com/viniciuswerneck/winforge/releases/latest/download/WinForge-Setup-1.0.0.0.exe">
    <img src="https://img.shields.io/badge/Baixar-Instalador-blue?style=for-the-badge&logo=windows" alt="Baixar Instalador" />
  </a>
</div>

---

## Sobre

**WinForge** é uma interface gráfica moderna para o [winget](https://learn.microsoft.com/pt-br/windows/package-manager/winget/) — o gerenciador de pacotes nativo do Windows. Construído com .NET 9 e WPF, o WinForge oferece uma experiência visual alinhada ao **Fluent Design do Windows 11**, permitindo que você gerencie aplicativos instalados, descubra novos pacotes e mantenha tudo atualizado sem precisar usar a linha de comando.

### Funcionalidades

| Funcionalidade | Descrição |
|----------------|-----------|
| Pacotes Instalados | Lista todos os pacotes gerenciados pelo winget com busca em tempo real e filtro por repositório |
| Busca & Instalar | Encontre pacotes na comunidade winget e Microsoft Store com busca automática enquanto digita |
| Atualizações | Veja atualizações disponíveis e aplique-as individualmente ou em massa com um clique |
| Tema Escuro/Claro | Alterna entre temas dark e light; detecta automaticamente o tema do Windows |
| Atalhos de Teclado | `Ctrl+F` busca, `Ctrl+L` recarrega lista, `Enter` confirma |
| Onboarding | Tela de boas-vindas na primeira execução explicando o funcionamento |

---

## Instalação

### Instalador Windows (recomendado)

<a href="https://github.com/viniciuswerneck/winforge/releases/latest/download/WinForge-Setup-1.0.0.0.exe">
  <img src="https://img.shields.io/badge/Baixar-WinForge--Setup--1.0.0.0.exe-blue?style=for-the-badge" alt="Baixar Instalador" />
</a>

1. Clique no botão acima para baixar o instalador
2. Execute o arquivo `WinForge-Setup-1.0.0.0.exe`
3. Siga as instruções na tela
4. O WinForge será instalado em `%LOCALAPPDATA%\Programs\WinForge`
5. Um atalho será criado no Menu Iniciar

> O instalador verifica automaticamente se o .NET 9 Desktop Runtime está instalado. Se não estiver, ele exibe um aviso com o link para download.

### Pré-requisitos

- **Windows 10** (build 17763+) ou **Windows 11**
- **winget** — já incluso no Windows 11 e no App Installer do Windows 10
- **.NET 9 Desktop Runtime** — [baixar aqui](https://dotnet.microsoft.com/download/dotnet/9.0)

### Via linha de comando

```powershell
# Clone o repositório
git clone https://github.com/viniciuswerneck/winforge.git
cd winforge

# Execute
dotnet run --project WinForge\WinForge.csproj
```

### Build manual

```powershell
# Compilar
dotnet build WinForge\WinForge.csproj --force

# Executar
Start-Process -WindowStyle Normal -FilePath "dotnet" -ArgumentList "run --project WinForge\WinForge.csproj" -WorkingDirectory $pwd
```

> Se o build falhar com arquivos bloqueados, mate processos anteriores:
> `Get-Process WinForge -ErrorAction SilentlyContinue | Stop-Process -Force`

---

## Como Usar

### Primeira execução

Ao abrir o WinForge pela primeira vez, uma tela de **onboarding** apresenta as principais funcionalidades. Clique em **"Começar"** para prosseguir.

### Aba Instalados

- Veja todos os pacotes gerenciados pelo winget
- **Filtre** pelo campo de busca ou pelo dropdown de repositório (winget, msstore, etc.)
- Clique em **"Desinstalar"** para remover um pacote

### Aba Buscar & Instalar

- Digite o nome do pacote — os resultados aparecem automaticamente (300ms de debounce)
- Pressione **Enter** para busca imediata
- Clique em **"Instalar"** no card do pacote desejado
- Use o **✕** para limpar a busca

### Aba Atualizações

- Veja todos os pacotes com atualização disponível
- Clique em **"Atualizar"** em um pacote específico
- Use **"Atualizar Todos"** para aplicar todas as atualizações de uma vez

### Atalhos

| Atalho | Ação |
|--------|------|
| `Ctrl+F` | Inicia busca na aba atual |
| `Ctrl+L` | Recarrega lista de instalados |
| `Enter` | Confirma busca (no campo de texto) |

---

## Arquitetura

```
WinForge/
├── Models/
│   └── PackageInfo.cs          # Modelo de dados do pacote
├── Services/
│   ├── IWingetService.cs       # Interface do serviço winget
│   ├── WingetService.cs        # Chamadas ao CLI winget com parsing
│   └── ThemeService.cs         # Gerenciamento de temas dark/light
├── ViewModels/
│   ├── MainViewModel.cs        # ViewModel principal com comandos
│   └── PackageViewModel.cs     # ViewModel de cada pacote (avatar colorido via FNV-1a)
├── Converters/
│   ├── InverseBoolConverter.cs
│   ├── IntToVisibilityConverter.cs
│   └── StringNotEmptyToVisibilityConverter.cs
├── Styles/
│   ├── ThemeDark.xaml          # Recursos do tema escuro
│   └── ThemeLight.xaml         # Recursos do tema claro
├── Installer/
│   ├── build-installer.ps1     # Script de build do instalador
│   └── WinForge Setup.iss      # Script Inno Setup
├── App.xaml / App.xaml.cs      # Ponto de entrada, DI manual
├── MainWindow.xaml / .cs       # Interface principal
└── WinForge.csproj             # .NET 9, WPF, CommunityToolkit.Mvvm
```

### Stack Tecnológica

| Tecnologia | Versão |
|------------|--------|
| .NET | 9.0 |
| WPF | net9.0-windows |
| CommunityToolkit.Mvvm | 8.4.0 |
| winget | >= 1.29 |
| Inno Setup | 6+ |

### Padrões

- **MVVM** com source generators do CommunityToolkit.Mvvm
- **Injeção de Dependência manual** (sem container)
- **Temas customizados** via `ResourceDictionary` com 25+ brush keys
- **Hash FNV-1a** para cores de avatar determinísticas
- **Timeout de 120s** com kill automático em operações winget

---

## Compilando o Instalador

```powershell
# Requer .NET 9 SDK e Inno Setup 6+
.\Installer\build-installer.ps1

# Modo self-contained (inclui .NET 9 Runtime, ~80MB)
.\Installer\build-installer.ps1 -SelfContained
```

O instalador será gerado em `WinForge/output/WinForge-Setup-1.0.0.0.exe`.

---

## Roadmap

- Painel de detalhes do pacote (descrição, homepage, licença)
- Operações em lote com checkboxes
- Exportar/Importar lista de pacotes (JSON)
- Minimizar para bandeja com notificação de atualizações
- Ordenação por nome/versão/source
- Modo compacto de visualização
- Animações fade/slide nos cards
- Menu de contexto (Copiar ID, Abrir Homepage)

---

## Contribuição

Somos **open source** e todos são bem-vindos!

Veja o guia completo em [CONTRIBUTING.md](CONTRIBUTING.md).

**Formas de contribuir:**
- Reportar bugs e sugerir funcionalidades via [issues](https://github.com/viniciuswerneck/winforge/issues)
- Submeter Pull Requests com melhorias
- Melhorar a documentação
- Compartilhar o projeto

---

## Licença

Distribuído sob licença **MIT**. Veja [LICENSE](LICENSE) para mais informações.

Você pode usar, copiar, modificar, distribuir e contribuir livremente.

---

<div align="center">
  <p>
    <a href="https://github.com/viniciuswerneck/winforge/graphs/contributors">
      <img src="https://img.shields.io/github/contributors/viniciuswerneck/winforge?style=flat" alt="Contribuidores" />
    </a>
    <a href="https://github.com/viniciuswerneck/winforge/issues">
      <img src="https://img.shields.io/github/issues/viniciuswerneck/winforge?style=flat" alt="Issues" />
    </a>
    <a href="https://github.com/viniciuswerneck/winforge/stargazers">
      <img src="https://img.shields.io/github/stars/viniciuswerneck/winforge?style=flat" alt="Estrelas" />
    </a>
  </p>
  <p>Desenvolvido por <a href="https://lab.werneck.dev.br/">Werneck Lab</a></p>
  <p>
    <a href="https://github.com/viniciuswerneck/winforge/issues">Reportar Bug</a>
    ·
    <a href="https://github.com/viniciuswerneck/winforge/issues">Sugerir Funcionalidade</a>
    ·
    <a href="CONTRIBUTING.md">Contribuir</a>
  </p>
</div>
