# Contribuindo para o WinForge

Obrigado por considerar contribuir com o WinForge! 🚀

## 📋 Código de Conduta

Este projeto adota um código de conduta baseado no [Contributor Covenant](https://www.contributor-covenant.org/). Ao participar, você mantém um ambiente respeitoso e colaborativo.

## 🐛 Reportando Bugs

1. Verifique se o bug já não foi reportado nas [issues](https://github.com/viniciuswerneck/winforge/issues)
2. Abra uma nova issue com o template de bug
3. Inclua:
   - Versão do WinForge
   - Versão do Windows
   - Versão do winget (`winget --version`)
   - Passos para reproduzir
   - Comportamento esperado vs real
   - Logs ou prints se possível

## 💡 Sugerindo Funcionalidades

1. Abra uma [issue](https://github.com/viniciuswerneck/winforge/issues) com o template de feature
2. Descreva o problema que você quer resolver
3. Explique como a funcionalidade ajuda

## 🔧 Pull Requests

### 1. Setup

```powershell
git clone https://github.com/viniciuswerneck/winforge.git
cd winforge
dotnet build WinForge\WinForge.csproj
```

### 2. Crie uma branch

```powershell
git checkout -b feat/nome-da-feature
```

Use prefixos:
- `feat/` — nova funcionalidade
- `fix/` — correção de bug
- `refactor/` — refatoração
- `docs/` — documentação
- `style/` — formatação, estilo

### 3. Padrões de Código

- **MVVM**: Mantenha a separação entre View (XAML) e ViewModel (C#)
- **CommunityToolkit.Mvvm**: Use source generators (`[ObservableProperty]`, `[RelayCommand]`)
- **XAML**: Prefira `DynamicResource` nos templates, `StaticResource` em recursos fixos
- **Temas**: Nunca use cores fixas — sempre as brush keys dos dicionários de tema
- **winget**: Sempre use `--exact --id --silent --force --disable-interactivity`
- **Async**: Todos os comandos de winget devem ser assíncronos com timeout

### 4. Commit

```powershell
git commit -m "tipo: mensagem clara e concisa"
```

Tipos: `feat`, `fix`, `refactor`, `docs`, `style`, `chore`

### 5. Push e PR

```powershell
git push origin feat/nome-da-feature
```

Abra um Pull Request para `master` com descrição clara do que mudou e por quê.

## 🧪 Testes

Antes de submeter:
```powershell
dotnet build WinForge\WinForge.csproj
# Verifique 0 warnings, 0 erros
```

## 🎨 Diretrizes de UI

- Siga o **Windows 11 Fluent Design** (cantos arredondados, sombras, tipografia Segoe UI)
- Respeite as brush keys dos temas (`ApplicationBackgroundBrush`, `CardBackgroundFillColorDefaultBrush`, etc.)
- Nunca adicione **WPF-UI** ou MahApps — conflita com os dicionários customizados
- Cubra os 4 estados de UX: **loading**, **empty**, **error**, **success**

## 📝 Checklist Final

- [ ] Compila sem warnings
- [ ] Testei com winget real
- [ ] Segui os padrões de código
- [ ] Atualizei a documentação se necessário
- [ ] Verifiquei temas escuro e claro

---

<div align="center">
  <p>Dúvidas? Abra uma <a href="https://github.com/viniciuswerneck/winforge/issues">issue</a></p>
</div>
