# Task: Adaptar a nova marca para os ícones de app/favicon (#80)

Card: https://github.com/RafaVargas1/Synclass/issues/80

## Ordem de execução

- [x] Geração: substitui `assets/images/{icon,favicon,splash-icon}.png` e `assets/images/android-icon-{foreground,background,monochrome}.png` pelo padrão de 5 barras da `Marca.tsx` (issue #79), mantendo nomes/dimensões/formatos já referenciados em `app.json`
- [x] Geração: atualiza `assets/expo.icon/Assets/expo-symbol 2.svg` (formato novo de ícone iOS/Icon Composer) com o mesmo padrão de barras
- [x] Validação: confere visualmente cada asset gerado (dimensões, canal alpha onde esperado, cor)
- [x] Validação: build web (`npx expo export --platform web` ou `expo start --web`) confirma que o favicon novo é referenciado sem erro

### Inconsistências encontradas

A RN do card descreve isso como "atualização dos assets existentes" — mas
`assets/images/*.png` e `assets/expo.icon/` continham só os ícones
placeholder padrão do template Expo (o "A" chevron azul/branco), nunca
customizados pra marca Synclass antes desta Task. Não é uma correção de
regressão, é a primeira vez que o app ganha um ícone de fato de marca.
Não bloqueia a Task (os critérios de aceite/técnicos continuam válidos —
substituir os mesmos arquivos, mesmos nomes/formatos), só ajusta a
expectativa: o resultado é criação nova, não um ajuste incremental de
algo já no estilo da marca.

Geração via script Python (PIL) desenhando as 5 barras diretamente como
retângulos — sem gradiente/sombra (o placeholder anterior tinha, mas
esses efeitos não fazem parte do design flat estabelecido em `Marca.tsx`/
`design-system.md`; manter consistência de linguagem visual prevalece
sobre replicar o acabamento do placeholder).
