---
description: Convenções de nomenclatura e codificação para C# 13, .NET 9 e WinUI 3.
author: Antigravity
version: 1.0
---

# Naming Conventions (NanopaintHub)

## 1. C# e Estrutura Geral
- **Interfaces**: Sempre prefixadas com `I` (ex: `IUnitOfWork`, `IDocumentGeneratorService`).
- **Classes/Records/Structs**: PascalCase (ex: `ProductRepository`).
- **Campos Privados (Fields)**: camelCase prefixado com underscore (ex: `_chromiumService`).
- **Propriedades Públicas**: PascalCase.
- **Métodos Assíncronos**: Devem ter sempre o sufixo `Async` (ex: `GeneratePdfAsync()`, `CommitAsync()`).

## 2. MVVM e Source Generators (CommunityToolkit.Mvvm)
- Quando usar o CommunityToolkit.Mvvm nas ViewModels:
  - Declare propriedades observáveis como campos privados camelCase decorados com `[ObservableProperty]`. O gerador de código criará a propriedade PascalCase automaticamente (ex: `[ObservableProperty] private string _templateName;` gera `TemplateName`).
  - Declare comandos através de métodos privados decorados com `[RelayCommand]`. O gerador de código criará a propriedade `NomeMetodoCommand` (ex: `[RelayCommand] private void Save()` gera `SaveCommand`).

## 3. UI Nativa (WinUI 3 & XAML)
- **Nomes de Ficheiros XAML**: 
  - Páginas devem ter o sufixo `Page` (ex: `TemplateEditorPage.xaml`).
  - ContentDialogs ou Flyouts customizadas devem ter o sufixo `Dialog` ou `Flyout`.
- **Event Handlers no Code Behind**: O sufixo padrão é o nome do controlo + o evento (ex: `SaveButton_Click`, `Pivot_SelectionChanged`). Evite código de negócio solto nestes métodos; eles devem apenas interagir com a UI ou invocar o ViewModel.
- **Identificadores XAML (x:Name)**: PascalCase, terminando com o tipo de controlo se for referenciado no Code Behind (ex: `MainPivot`, `PrimaryColorPicker`).
