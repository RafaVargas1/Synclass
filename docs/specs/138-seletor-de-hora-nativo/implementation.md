# Implementação: seletor de hora vira campo HH:mm mascarado (#138)

## Decisão de design (resolvida, não é pra DeepSeek escolher)

Trocar as duas listas roláveis (`ListaDeNumeros`, 24 + 60 botões) por um
único campo de texto com máscara progressiva `HH:mm`, seguindo **o mesmo
padrão já usado no repositório** para máscara de digitação:
`frontend/src/lib/validacaoContato.ts`, função `mascararContato(valor,
valorAnterior)`. Não invente um mecanismo de máscara novo — replique a
forma dessa função (parâmetros, uso de `valorAnterior` pra lidar com
backspace, retorno da string já formatada) pro caso de hora.

Motivo da escolha (pedido explícito do usuário, issue #138): "um campo que
aceita hh:mm seria melhor" — mais rápido de preencher que navegar 2 listas
de rolagem, e não depende de nenhum comportamento de plataforma (input
nativo de hora teria requerido um arquivo `.web.tsx` separado — descartado
por complexidade desnecessária frente ao pedido direto do usuário).

## Arquivos afetados

### Novo: `frontend/src/lib/mascararHora.ts`

Função pura, mesmo formato de `mascararContato`:

```ts
/**
 * Máscara progressiva do campo de hora (issue #138): mesmo padrão de
 * `mascararContato` (validacaoContato.ts) — aplicada a cada tecla,
 * usando `valorAnterior` pra lidar com backspace sobre caractere de
 * máscara (o ":" não muda a contagem de dígitos).
 *
 * Restringe cada dígito à faixa válida no momento da digitação (não deixa
 * digitar hora > 23 nem minuto > 59) em vez de validar só no fim — mesmo
 * princípio de "prevenção de erro" já aplicado em outros campos do app.
 */
export function mascararHora(valor: string, valorAnterior = ''): string {
  let digitos = valor.replace(/\D/g, '').slice(0, 4);
  const digitosAnteriores = valorAnterior.replace(/\D/g, '');
  const apagouCaractere = valor.length < valorAnterior.length;
  if (apagouCaractere && digitos.length > 0 && digitos.length === digitosAnteriores.length) {
    digitos = digitos.slice(0, -1);
  }

  if (digitos.length >= 1) {
    const primeiroDigitoHora = Number(digitos[0]);
    if (primeiroDigitoHora > 2) {
      digitos = '2' + digitos.slice(1);
    }
  }
  if (digitos.length >= 2) {
    const hora = Number(digitos.slice(0, 2));
    if (hora > 23) {
      digitos = '23' + digitos.slice(2);
    }
  }
  if (digitos.length >= 3) {
    const terceiroDigito = Number(digitos[2]);
    if (terceiroDigito > 5) {
      digitos = digitos.slice(0, 2) + '5' + digitos.slice(3);
    }
  }

  return formatarHoraParcial(digitos);
}

function formatarHoraParcial(digitos: string): string {
  if (digitos.length === 0) return '';
  if (digitos.length <= 2) return digitos;
  return `${digitos.slice(0, 2)}:${digitos.slice(2)}`;
}

/** HH:mm completo (4 dígitos) — só então é um valor válido pra confirmar. */
export function horaEstaCompleta(valor: string): boolean {
  return /^\d{2}:\d{2}$/.test(valor);
}
```

Teste (`frontend/src/lib/mascararHora.test.ts`), espelhando os casos já
cobertos por `validacaoContato.test.ts` pra `mascararContato`:
- `''` → `''`
- `'1'` → `'1'` (ainda não fecha a hora)
- `'9'` → `'2'` (1º dígito de hora nunca pode ser >2 — clamp imediato)
- `'0930'` (digitado de uma vez, simulando `fireEvent.changeText` direto
  com a string final) → `'09:30'`
- `'1330'` → `'13:30'`; `'2530'` → `'23:30'` (clamp no 2º dígito: `25`
  vira `23`)
- `'1099'` → `'10:59'` (clamp de minuto no 3º dígito: `9` > 5 vira `5`)
- Backspace sobre o `:` remove o dígito anterior a ele (mesmo
  comportamento de `mascararContato` pro caractere de máscara).
- `horaEstaCompleta('10:30')` → `true`; `horaEstaCompleta('10:3')` → `false`;
  `horaEstaCompleta('')` → `false`.

### Reescrito: `frontend/src/components/molecules/SeletorDeHora.tsx`

Remove `ListaDeNumeros`, `ScrollView`, o estado `aberto`/`horaSelecionada`/
`minutoSelecionado`, e o botão "Confirmar". Vira um campo de texto único:

```tsx
import { Text, View } from 'react-native';

import { Input } from '@/components/atoms/Input';
import { horaEstaCompleta, mascararHora } from '@/lib/mascararHora';

export type SeletorDeHoraProps = {
  label: string;
  valor: string | undefined;
  onSelecionar: (hora: string) => void;
};

/**
 * Molécula: campo de hora com máscara progressiva HH:mm (issue #138,
 * substitui o painel de 2 listas roláveis da issue #117 — não era um
 * padrão de seleção de hora reconhecível, ver ux-heuristics.md#reconhecimento-em-vez-de-recordação).
 * Só chama `onSelecionar` quando o valor tem os 4 dígitos completos —
 * mesmo princípio de "sem pré-seleção acidental" da versão anterior
 * (achado de dev-review, PR #120): um valor parcial nunca é reportado
 * como escolhido.
 */
export function SeletorDeHora({ label, valor, onSelecionar }: SeletorDeHoraProps) {
  function handleChangeText(digitado: string) {
    const mascarado = mascararHora(digitado, valor ?? '');
    if (horaEstaCompleta(mascarado)) {
      onSelecionar(mascarado);
      return;
    }
    // Valor parcial: repropaga como está, sem chamar onSelecionar — o
    // componente é controlado pelo `valor` do pai, então precisamos de
    // um jeito de mostrar o parcial antes de completar. Ver "Edge point"
    // abaixo — decisão: onSelecionar também recebe o parcial, e quem
    // consome (HorarioForm) já trata valor indefinido/incompleto como
    // "ainda não escolhido" nas suas próprias validações.
    onSelecionar(mascarado);
  }

  return (
    <View className="gap-one">
      <Text className="text-sm font-medium text-text dark:text-dark-text">{label}</Text>
      <Input
        value={valor ?? ''}
        onChangeText={handleChangeText}
        placeholder="HH:mm"
        keyboardType="numeric"
        maxLength={5}
        accessibilityLabel={label}
      />
    </View>
  );
}
```

## Edge point: valor parcial propagado pro pai

Diferente da versão anterior (só chamava `onSelecionar` com um HH:mm
completo, nunca parcial), este desenho propaga QUALQUER valor mascarado
pro pai, completo ou não — necessário porque o campo agora é controlado
(`value={valor}`) e precisa mostrar o que o usuário está digitando antes
de completar. **Consequência que precisa de ajuste em quem consome**:
`HorarioForm.tsx` (`frontend/src/components/organisms/HorarioForm.tsx`,
função `validar`) checa hoje `horaInicio === undefined` pra decidir se o
usuário "não escolheu hora" — com o novo componente, um valor parcial tipo
`'10:3'` não é `undefined`, é uma string incompleta. Troque a checagem em
`validar` de `horaInicio === undefined` para
`!horaEstaCompleta(horaInicio ?? '')` (importar `horaEstaCompleta` de
`@/lib/mascararHora`), reaproveitando a mesma mensagem
`MensagemHoraObrigatoria`. Atualize `HorarioForm.test.tsx` se algum teste
depender do formato exato do estado interno de hora.

## Teste do componente (`SeletorDeHora.test.tsx`, reescrito do zero)

Substitua TODOS os testes atuais (eles testam o painel de 2 listas, que
deixa de existir) por:
- Mostra o `label`.
- Mostra `valor` quando fornecido (`value` do `Input`, não mais `<Text>`).
- Digitar `'1030'` chama `onSelecionar` com `'10:30'` no final (pode
  simular via `fireEvent.changeText` sequencial ou de uma vez, já que
  `mascararHora` é determinística sobre a string completa também).
- Digitar um valor parcial (ex: `'10'`) NÃO deixa `onSelecionar` receber
  algo que passa em `horaEstaCompleta` — teste indireto: renderize com
  esse valor parcial e confirme que `horaEstaCompleta(valorRecebido)` é
  `false` no último `onSelecionar` chamado.
- `accessibilityLabel` do campo é o `label` recebido.

## Fora de escopo

- Não mexer em `SeletorDeData.tsx` (calendário) — padrão diferente,
  já funciona bem, fora do pedido do usuário.
- Não criar variante `.web.tsx` com `<input type="time">` — decisão já
  tomada acima (campo mascarado atende o pedido do usuário sem a
  complexidade de um arquivo por plataforma).
- Não mexer em nenhum outro formulário que não seja `HorarioForm.tsx`
  (é o único que usa `SeletorDeHora` hoje — confirme com
  `grep -rn "SeletorDeHora" frontend/src` antes de começar).
