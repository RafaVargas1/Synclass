Mudanças em relação ao `palette.js` atual: `background` sai de branco/preto
puro; `primary` no light escurece de `#208AEF` para `#1873BD` — o tom atual
tem contraste apertado quando usado como texto/link sobre fundo claro (ex:
"Já tenho conta — Entrar" em `HomeHero`), e vai continuar servindo bem como
fundo de botão com texto branco. O valor de `background` no modo claro foi
clareado de `#F5F6F8` para `#F9FAFB` e validado por teste automatizado
(`frontend/src/theme/palette.test.ts`): a razão de contraste contra
`backgroundElement` (`#E7E9ED`) e `backgroundSelected` (`#D8DBE1`) permanece
>= 1.10 par a par (a escada de elevação fica mais perceptível, não menos), e
o contraste de `text` (`#14161A`) e `text-secondary` (`#5B616B`) sobre o novo
fundo permanece >= 4.5:1 (AA). `border` é um token novo — não existe hoje
porque nada precisava de linha divisória com sombra disponível; agora
precisa.

Não adicionar cor de "sucesso" enquanto nenhuma tela precisar dela — hoje
`LoginConfirmado`/`CadastroConfirmado` usam texto normal, e isso está certo.
Quando surgir a necessidade, um verde equivalente em saturação ao `primary`
atual, nunca pastel.
