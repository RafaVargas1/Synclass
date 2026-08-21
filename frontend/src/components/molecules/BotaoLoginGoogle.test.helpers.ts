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

export function BotaoLoginGoogleDeTeste(props: BotaoLoginGoogleProps) {
  botaoProps = props;
  return null;
}
