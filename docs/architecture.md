# Arquitetura

HeavyShot é uma implementação independente. A investigação do Lightshot orientou a separação conceitual de captura, edição e saída; nenhum código descompilado foi reutilizado.

- `Program.cs`: processo único por sessão, janela principal, atalhos, bandeja e preferências XML portáteis.
- `Native.cs`: APIs Windows para atalhos, DPI e captura opcional do cursor. `Graphics.CopyFromScreen` captura a área virtual, incluindo origens negativas.
- `CaptureForm.cs`: sobreposição sobre bitmap congelado, seleção, barras, interação e saídas.
- `Drawing.cs`: histórico de anotações e composição no tamanho original. A exportação recorta a imagem base e traduz as coordenadas das anotações, sem capturar os controles ou o escurecimento da sobreposição.
- `Theme.cs`: controles e cores próprios.
- `SelfTest.cs`: verificações de renderização, histórico, recorte e interação via eventos; também produz a imagem de demonstração.

O modelo de documento é o dono do bitmap capturado; a janela usa esse bitmap enquanto aberta. Ao encerrar o editor, ambos são descartados pelo chamador. Anotações guardam dados vetoriais até a exportação. Refazer é invalidado quando uma nova anotação é adicionada após desfazer.

O aplicativo grava apenas preferências ao lado do executável e arquivos de imagem em locais escolhidos pelo usuário. A captura permanece em memória até salvar/copiar. Não há módulo de rede, instalador, tarefa agendada ou alteração de registro para autoinicialização.

Referência da API de captura: https://learn.microsoft.com/en-us/dotnet/api/system.drawing.graphics.copyfromscreen
