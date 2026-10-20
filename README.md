# HeavyShot

Captura de tela e anotação para Windows, em versão **portable**. Extraia e execute, sem instalador e sem administrador.

## Primeira versão — 0.1.0

- Captura da área virtual dos monitores e seleção por arraste.
- Seleção com movimentação e alças para redimensionar.
- Caneta, marcador, linha, seta, retângulo, elipse e texto.
- Cores, espessura e desfazer/refazer.
- Copiar a imagem ou salvar em PNG, JPEG e BMP.
- Atalho global Ctrl+Shift+S e Print Screen opcional.
- Captura opcional do cursor, bandeja e preferências ao lado do executável.
- Processamento local, sem upload ou telemetria.

## Usar

Baixe `HeavyShot-0.1.0-portable.zip` nos Releases, extraia e execute `HeavyShot.exe`.
Requer Windows 10/11 com .NET Framework 4.8. O aplicativo não precisa do SDK .NET.

Clique em **Nova captura**, selecione uma área e use a barra flutuante. **Ctrl+C** ou **Enter** copia; **Ctrl+S** salva; **Esc** cancela.

| Tecla | Ferramenta |
|---|---|
| V | Seleção: mover e redimensionar |
| P / M | Caneta / marcador |
| L / A | Linha / seta |
| R / E | Retângulo / elipse |
| T | Texto |
| Ctrl+Z / Ctrl+Y | Desfazer / refazer |
| Ctrl+A | Selecionar tela inteira |

Fechar a janela principal mantém o HeavyShot na bandeja. Use **Sair** no menu da bandeja para encerrar. Inicie com `--tray` para abrir diretamente na bandeja. O programa não configura inicialização automática.

As preferências ficam em `HeavyShot.settings.xml`, na mesma pasta do executável. Se a pasta for somente leitura, o aplicativo continua usando as opções em memória. Para remover, encerre o programa e apague a pasta.

## Compilar e validar

No PowerShell do Windows, usando o compilador .NET Framework disponível no sistema:

```powershell
.\build.ps1 -Test -Package
.\test-capture.ps1
```

O executável é produzido em `dist/HeavyShot.exe`, os resultados ficam em `tests/results/` e o ZIP portátil na raiz. Os testes cobrem pixels do recorte, anotações, histórico, exportação PNG/JPEG e eventos do editor. `--demo` abre o editor sobre uma imagem sintética, sem capturar a tela, para revisão visual.

`test-capture.ps1` verifica a captura real com e sem cursor em uma sessão Windows com desktop disponível, sem salvar a imagem. Veja [validação e limites](docs/validation.md).

## Arquitetura e limites

Implementação própria em C# / WinForms / GDI+, inspirada no fluxo de captura analisado do Lightshot. Nenhum código, ícone, recurso ou DLL proprietário foi incorporado. Veja [arquitetura](docs/architecture.md).

Esta versão usa captura GDI. Conteúdo protegido, jogos em tela exclusiva, HDR e cenários mistos de DPI precisam de validação adicional; não há backend DXGI nesta versão. Upload, impressão, atualização automática e edição de anotações já concluídas ficam para versões futuras. As anotações ficam nas coordenadas da imagem original quando a seleção é movida. O executável ainda não possui assinatura digital.
