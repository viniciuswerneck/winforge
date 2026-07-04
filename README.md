<div align="center">
  <img src="WinForge/icon.ico" alt="WinForge" width="80" height="80" />
  <h1>WinForge</h1>
  <p><strong>Gerenciador de Pacotes winget com Interface Gráfica Moderna</strong></p>
  <p>
    <img src="https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet" alt=".NET 9" />
    <img src="https://img.shields.io/badge/WPF-net9.0--windows-512BD4?logo=windows" alt="WPF" />
    <img src="https://img.shields.io/github/v/tag/viniciuswerneck/winforge?label=versão" alt="Version" />
    <img src="https://img.shields.io/badge/licença-MIT-green" alt="License" />
  </p>
</div>

---

## 📋 Sobre

**WinForge** é uma interface gráfica moderna para o [winget](https://learn.microsoft.com/pt-br/windows/package-manager/winget/) — o gerenciador de pacotes nativo do Windows. Construído com .NET 9 e WPF, o WinForge oferece uma experiência visual alinhada ao **Fluent Design do Windows 11**, permitindo que você gerencie aplicativos instalados, descubra novos pacotes e mantenha tudo atualizado sem precisar usar a linha de comando.

### ✨ Funcionalidades

| Funcionalidade | Descrição |
|----------------|-----------|
| **📦 Pacotes Instalados** | Lista todos os pacotes gerenciados pelo winget com busca em tempo real e filtro por repositório (winget / Microsoft Store) |
| **🔍 Busca & Instalar** | Encontre pacotes na comunidade winget e Microsoft Store com busca automática enquanto digita (debounce 300ms) |
| **⬆️ Atualizações** | Veja atualizações disponíveis e aplique-as individualmente ou em massa com um clique |
| **🌓 Tema Escuro/Claro** | Alterna entre temas dark e light; detecta automaticamente o tema do Windows |
| **⌨️ Atalhos de Teclado** | `Ctrl+F` busca, `Ctrl+L` recarrega lista, `Enter` confirma |
| **👋 Onboarding** | Tela de boas-vindas na primeira execução explicando o funcionamento |

---

## 🖼️ Capturas de Tela

| Tema Escuro | Tema Claro |
|:-----------:|:----------:|
| *(adicione aqui)* | *(adicione aqui)* |

---

## 🚀 Instalação

### Pré-requisitos

- **Windows 10** (build 17763+) ou **Windows 11**
- **winget** — já incluso no Windows 11 e no App Installer do Windows 10
- **.NET 9 Runtime** — [baixar aqui](https://dotnet.microsoft.com/download/dotnet/9.0)

### Via GitHub (recomendado)

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

## 🎮 Como Usar

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

## 🏗️ Arquitetura

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
| winget | ≥ 1.29 |

### Padrões

- **MVVM** com source generators do CommunityToolkit.Mvvm
- **Injeção de Dependência manual** (sem container)
- **Temas customizados** via `ResourceDictionary` com 25+ brush keys
- **Hash FNV-1a** para cores de avatar determinísticas
- **Timeout de 120s** com kill automático em operações winget

---

## ⚙️ Integração com winget

O WinForge executa o winget nos bastidores com os seguintes parâmetros:

| Operação | Comando |
|----------|---------|
| Listar instalados | `winget list --accept-source-agreements` |
| Buscar pacotes | `winget search <query> --accept-source-agreements` |
| Instalar | `winget install --exact --id <id> --silent --force --disable-interactivity --accept-source-agreements --accept-package-agreements` |
| Atualizar | `winget upgrade --exact --id <id> --silent --force --disable-interactivity --accept-source-agreements --accept-package-agreements` |
| Atualizar todos | `winget upgrade --all --silent --force --disable-interactivity --accept-source-agreements --accept-package-agreements` |
| Desinstalar | `winget uninstall --exact --id <id> --silent --force --disable-interactivity --accept-source-agreements` |

> Todos os comandos utilizam `--silent --force --disable-interactivity` para evitar bloqueios por diálogos do instalador.

---

## 🧪 Compilação do Zero

```powershell
# Requer .NET 9 SDK
dotnet new wpf -n WinForge
cd WinForge
dotnet add package CommunityToolkit.Mvvm --version 8.4.0
# Copie os arquivos do repositório
dotnet build
dotnet run
```

---

## 🗺️ Roadmap

- [ ] **Painel de detalhes** do pacote (descrição, homepage, licença)
- [ ] **Operações em lote** com checkboxes
- [ ] **Exportar/Importar** lista de pacotes (JSON)
- [ ] **Minimizar para bandeja** com notificação de atualizações
- [ ] **Ordenação** por nome/versão/source
- [ ] **Modo compacto** de visualização
- [ ] **Animações** fade/slide nos cards
- [ ] **Menu de contexto** (Copiar ID, Abrir Homepage)

---

## 🤝 Contribuição

Contribuições são bem-vindas! Siga os passos:

1. Fork o projeto
2. Crie sua branch: `git checkout -b feature/nova-funcionalidade`
3. Commit suas mudanças: `git commit -m 'feat: adiciona nova funcionalidade'`
4. Push para a branch: `git push origin feature/nova-funcionalidade`
5. Abra um Pull Request

---

## 📄 Licença

Distribuído sob licença MIT. Veja [LICENSE](LICENSE) para mais informações.

---

<div align="center">
  <p>Desenvolvido por <a href="https://lab.werneck.dev.br/">Werneck Lab</a></p>
  <p>
    <a href="https://github.com/viniciuswerneck/winforge/issues">Reportar Bug</a>
    ·
    <a href="https://github.com/viniciuswerneck/winforge/issues">Sugerir Funcionalidade</a>
  </p>
</div>
