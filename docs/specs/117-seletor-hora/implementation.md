# Implementação: SeletorDeHora (#117)

## Componentes afetados

- **Novo** `frontend/src/components/molecules/SeletorDeHora.tsx`:
  mesma estrutura de `SeletorDeData.tsx` (estado `aberto`, `Pressable`
  que alterna, fecha ao selecionar) — painel interno com duas
  `ScrollView horizontal` (Hora 00-23, Minuto 00-59), cada opção um
  `Pressable` numerado com `minWidth`/`minHeight` de 44 (issue #115) e
  destaque visual (`border-primary`/`bg-primary`, mesmo padrão de
  `ChipSelector`/`SeletorDeData`) quando selecionado.
- **Modificado** `frontend/src/components/organisms/HorarioForm.tsx`:
  troca o `FormField` de "Hora de início" por `SeletorDeHora`; remove a
  validação de formato (regex `/^\d{2}:\d{2}$/`) do `validar()` interno —
  não é mais possível chegar num valor inválido, então validar formato
  de novo seria código morto.

## Por que duas listas roláveis em vez de `ChipSelector` com `flex-wrap`

`ChipSelector` já existe e resolve seleção única entre poucas opções
nomeadas (dia da semana, política de marcação) — 60 opções numéricas
(minutos) em `flex-wrap` quebraria em várias linhas e ficaria difícil de
escanear. Duas listas roláveis horizontais (como um seletor tipo "roda")
mantêm cada opção com alvo de toque generoso sem estourar a altura da
tela, e são um padrão já familiar (relógios/pickers nativos usam a mesma
ideia).

## Contrato de API

Nenhum — `criarHorario` continua recebendo `horaInicio` como
`HH:mm:00`, só a captura do valor muda.

## Testes

`SeletorDeHora.test.tsx`: cobre exibição do valor formatado, abertura do
painel, seleção de hora+minuto chamando `onSelecionar('HH:mm')`, e alvo
de toque dos botões internos. `HorarioForm.test.tsx`: ajusta o teste que
hoje simula digitação em "Hora de início" pra simular seleção via
`SeletorDeHora` (mock, mesmo padrão usado para `SeletorDeData` em
`professor/[professorId]/valor-devido.test.tsx`).
