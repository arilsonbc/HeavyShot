# Validação de 0.1.0

- Compilação .NET Framework concluída, sem erros ou avisos do compilador.
- 21 verificações automatizadas aprovadas: direção da seleção, recorte, coordenadas, limites, seleção vazia, renderização de sete ferramentas, desfazer/refazer, invalidação do refazer, eventos de arraste/movimentação/desenho e reabertura de PNG/JPEG.
- Prévia do editor renderizada com barras de ferramentas sobre imagem sintética e inspecionada visualmente.
- Janela principal aberta no desktop e inspecionada.
- Captura real em memória validada em 3000 × 1920, com opção de cursor desabilitada e habilitada. Amostras de pixels variadas e alpha 255. Nenhuma imagem do desktop foi salva ou enviada.
- ZIP extraído em outra pasta; executável com SHA-256 idêntico e os testes executados novamente no pacote extraído.

A checagem real encontrou e permitiu corrigir um problema que os testes sintéticos não cobriam: `Graphics.CopyFromScreen` do .NET Framework rejeita a combinação das flags `SourceCopy | CaptureBlt`. O produto usa agora a chamada Win32 `BitBlt` diretamente. O teste reproduzível `test-capture.ps1` cobre essa integração e verifica também a opacidade dos pixels.

Para repetir:

```powershell
.\build.ps1 -Test -Package
.\test-capture.ps1
```

O segundo comando exige uma sessão Windows com desktop disponível. Ele mantém a imagem somente em memória e imprime apenas dimensões, contagem de amostras e resultado. Não prova que o cursor visível foi desenhado numa posição específica: confirma que os dois caminhos retornam uma imagem válida.

Ainda sem validação completa: clipboard entre aplicativos, impressão (não implementada), múltiplos monitores com escalas distintas, HDR, conteúdo protegido e jogos em tela exclusiva. Não foi concluído um teste manual de todas as ferramentas na interface. As verificações de arraste usam os eventos reais do formulário, acionados pelo teste automatizado.
