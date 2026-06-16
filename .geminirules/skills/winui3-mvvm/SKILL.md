---
name: winui3-mvvm
description: Boas práticas de integração, vinculação de dados (Binding), geradores de código e gestão correta de Popups / Flyouts ao codificar Views no ambiente Nativo Windows App SDK.
---

# SKILL: WinUI 3 & MVVM Toolkit (NanopaintHub)

Esta _skill_ capacita o agente Antigravity a escrever código e ecrãs WinUI 3 (Windows App SDK 1.6+) consistentes, responsivos, à prova de exceções de concorrência e conformes aos requisitos de Clean Architecture.

## 1. Binding XAML com x:Bind
Sempre que desenhar XAML, use a extensão de marcação fortemente tipada `x:Bind` (compilada) no lugar da sintaxe legada `Binding` baseada em reflexão.
- Adicione `x:DataType="models:MeuModelo"` a todas as `DataTemplate` de `ListView` ou `GridView`.
- Lembre-se: `x:Bind` usa por defeito o `Mode=OneTime`. Se quer leitura reativa, use `Mode=OneWay`. Se a origem é um input do utilizador (como TextBox ou ColorPicker), use ESTRITAMENTE `Mode=TwoWay`.

## 2. Modelos de Vista Reativos com Source Generators
Ao escrever ViewModels na pasta `ViewModels/`:
- Devem herdar sempre de `ObservableObject` do `CommunityToolkit.Mvvm.ComponentModel`.
- Propriedades com getters e setters completos são código boilerplate inútil e perigoso. Em vez disso, aplique `[ObservableProperty]` a um campo privado `_minhaVariavel`.

## 3. Comportamento Seguro de Popups e Flyouts (O Problema XamlRoot)
Ao contrário do WPF clássico ou UWP antigo, no WinUI 3 Popups podem ficar fora da árvore visual do parente que os evocou.
**Padrão de Resolução para fechar `Flyout` a partir de um botão interior no Code Behind**:
```csharp
if (sender is Microsoft.UI.Xaml.FrameworkElement fe && fe.XamlRoot != null)
{
    var popups = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(fe.XamlRoot);
    foreach (var popup in popups)
    {
        popup.IsOpen = false; 
    }
}
```
Use SEMPRE o método estático de recolha ao `XamlRoot` principal em vez de subir forçosamente o cast da `Parent`.
