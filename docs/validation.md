# Validação de 0.2.0

- Interface baseada no layout editável do Figma: https://www.figma.com/design/prmFuOyE1k4FQKpfj5i2UZ?node-id=3-37
- Painéis claros e arredondados; barra vertical junto à seleção; ações abaixo dela; ícones desenhados na grade de 24 px; estados ativo/inativo e painel de cor, espessura e opacidade.
- Adaptação nativa para WinForms com Segoe UI, seletores de formato e espessura nativos do Windows. As prévias não são usadas como imagens de interface.
- 34 verificações automatizadas aprovadas, incluindo renderização da opacidade, barras em três posições de borda, ausência de sobreposição e interação com o painel de ajustes.
- Prévias do editor e do painel de cor renderizadas a partir do formulário executável e inspecionadas visualmente.
- Captura real em memória em 1920 × 1080 validada com e sem cursor; pixels variados e opacos, sem gravação da imagem do desktop.
- Pacote portátil extraído em outra pasta e testes repetidos; hash do executável conferido.

Limites: não houve teste manual completo do clipboard entre aplicativos nem validação com monitores de DPI misto/HDR. No desktop Windows usado para captura, o painel tem o espaço disponível; resoluções com altura menor que a barra vertical (534 px) exigem adaptação adicional. O projeto continua portátil, sem instalador ou assinatura digital.

## Histórico — 0.1.0

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
