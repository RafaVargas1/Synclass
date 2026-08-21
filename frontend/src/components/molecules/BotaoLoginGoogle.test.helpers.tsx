import { Text } from 'react-native';

import type {
  BotaoLoginGoogleProps,
} from '@/components/molecules/BotaoLoginGoogle';

/**
 * Variável que captura as props recebidas pela última renderização do
 * `BotaoLoginGoogle` mockado nas telas — permite aos testes das telas
 * dispararem os callbacks (`onAutenticado`/`onCadastroPendente`) sem depender
 * do fluxo interno (Google Sign-In + Api), que já é coberto pelo teste
 * próprio do componente. Expoja no módulo de helpers porque `jest.fn()`
 * declarado dentro de `jest.mock` não é alcançável pelo corpo do teste.
 */
export let botaoProps: BotaoLoginGoogleProps;

/**
 * Renderiza o rótulo real (não `null`) pra testes de hierarquia/ordem
 * (ex: `HomeHero`, issue #111) conseguirem localizar o botão via
 * `getByText`/`getByRole` sem precisar do fluxo interno do Google.
 */
export function BotaoLoginGoogleDeTeste(props: BotaoLoginGoogleProps) {
  // eslint-disable-next-line react-hooks/globals -- substituto de mock só de teste, nunca roda em produção; é o único jeito de os testes das telas acionarem os callbacks sem duplicar o fluxo do Google.
  botaoProps = props;
  return <Text accessibilityRole="button">Entrar com Google</Text>;
}
