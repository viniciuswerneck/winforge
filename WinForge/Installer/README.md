# WinForge - Gerador de Instalador

## Pré-requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Inno Setup 6+](https://jrsoftware.org/isdl.php)

## Como usar

### Opção 1: Build completo (recomendado)

```powershell
.\Installer\build-installer.ps1
```

Isso vai:
1. Publicar o WinForge em modo Release (framework-dependent)
2. Compilar o instalador com Inno Setup
3. Gerar `output/WinForge-Setup-1.0.0.exe`

### Opção 2: Build self-contained

```powershell
.\Installer\build-installer.ps1 -SelfContained
```

Gera um instalador maior (~80-100MB) que inclui o .NET 9 Runtime. Não precisa de pré-requisitos no PC do usuário.

### Opção 3: Compilar apenas o instalador

Se já tem a publicação pronta na pasta `output/`:

```powershell
iscc.exe "Installer\WinForge Setup.iss"
```

## Estrutura de saída

Após o build, a pasta `output/` conterá:

```
output/
├── WinForge-Setup-1.0.0.exe    # Instalador (arquivo único para distribuição)
├── WinForge.exe                 # Executável
├── WinForge.dll                 # Assembly principal
├── CommunityToolkit.Mvvm.dll    # Dependência NuGet
├── WinForge.deps.json           # Manifesto de dependências
├── WinForge.runtimeconfig.json  # Configuração de runtime
└── icon.ico                     # Ícone do app
```

## Distribuição

O arquivo `WinForge-Setup-1.0.0.exe` é o único que precisa ser distribuído. Os usuários podem baixá-lo e executar diretamente.

## Customização

### Alterar versão

Edite a variável `$Version` no script `build-installer.ps1` e `#define MyAppVersion` no arquivo `.iss`.

### Alterar idiomas

No arquivo `.iss`, na seção `[Languages]`, adicione ou remova idiomas. Os arquivos `.isl` do Inno Setup ficam em `compiler:Languages/`.

### Ícone personalizado

Substitua o arquivo `icon.ico` na raiz do projeto WinForge.

## Solução de problemas

### "Inno Setup não encontrado"

Instale o Inno Setup 6 em https://jrsoftware.org/isdl.php ou adicione `iscc.exe` ao PATH do sistema.

### "dotnet SDK não encontrado"

Instale o .NET 9 SDK em https://dotnet.microsoft.com/download/dotnet/9.0.

### "Falha ao publicar"

Verifique se não há erros de compilação no projeto:
```powershell
dotnet build WinForge\WinForge.csproj --force
```

### Instalador trava na verificação de runtime

O verificador de .NET 9 Runtime usa `dotnet --list-runtimes`. Se o dotnet não estiver no PATH, a verificação pode falhar. Instale o .NET 9 Runtime Desktop em https://dotnet.microsoft.com/download/dotnet/9.0/runtime.
