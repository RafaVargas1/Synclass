import { Text } from 'react-native';

import type { BotaoLoginAppleProps } from '@/components/molecules/BotaoLoginApple';

/**
 * Variável que captura as props recebidas pela última renderização do
 * `BotaoLoginApple` mockado nas telas — permite aos testes das telas
 * dispararem os callbacks (`onAutenticado`/`onCadastroPendente`) sem depender
 * do fluxo interno (Sign in with Apple + Api), que já seria coberto pelo
 * teste próprio do componente. Espelho de `BotaoLoginGoogle.test.helpers.tsx`
 * — mesmo padrão, provedor diferente (issue #212).
 */
export let botaoAppleProps: BotaoLoginAppleProps;

/**
 * Renderiza o rótulo real (não `null`) pra testes de hierarquia/ordem
 * conseguirem localizar o botão via `getByText`/`getByRole` sem precisar do
 * fluxo interno da Apple. Mesmo padrão de `BotaoLoginGoogleDeTeste`.
 */
export function BotaoLoginAppleDeTeste(props: BotaoLoginAppleProps) {
  // eslint-disable-next-line react-hooks/globals -- substituto de mock só de teste, nunca roda em produção; é o único jeito de os testes das telas acionarem os callbacks sem duplicar o fluxo da Apple.
  botaoAppleProps = props;
  return <Text accessibilityRole="button">Continuar com Apple</Text>;
}
