import * as AppleAuthentication from 'expo-apple-authentication';
import { useRef, useState } from 'react';
import { Platform, Pressable, Text, useColorScheme } from 'react-native';

import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { IconeApple } from '@/components/atoms/IconeApple';
import { loginComApple } from '@/lib/api/auth';
import { obterIdTokenApple } from '@/lib/auth/apple';

export type ResultadoAutenticadoApple = {
  token: string;
  nome: string;
  papeis: string[];
};

export type BotaoLoginAppleProps = {
  /**
   * Chamado quando o e-mail do idToken corresponde a um usuário existente —
   * carrega o resultado do login (token + papeis) para a tela definir a
   * sessão (ver `useSessao().definirSessao`).
   */
  onAutenticado: (resultado: ResultadoAutenticadoApple) => void;
  /**
   * Chamado quando o e-mail do idToken ainda não tem usuário correspondente
   * — carrega o e-mail normalizado para a tela levar o usuário ao cadastro.
   */
  onCadastroPendente: (email: string) => void;
};

/**
 * Molécula: botão único de entrada com conta Apple (issues #212 e #213),
 * reaproveitado nas mesmas telas que o `BotaoLoginGoogle` (Home, login,
 * cadastro Professor e cadastro Aluno). É a única que conhece o fluxo
 * completo da Apple (`obterIdTokenApple` → `loginComApple`), assim as telas
 * não duplicam a orquestração — a tela só decide o que fazer com o desfecho
 * via callback.
 *
 * Ramifica por plataforma, mesmo padrão de `obterIdTokenGoogle`/`google.ts`:
 * em iOS usa o `AppleAuthenticationButton` oficial do SDK
 * `expo-apple-authentication` (a Apple exige o componente oficial em apps
 * nativos, não um botão customizado — ver implementation.md#componente-de-botão-nativo),
 * em Android não renderiza nada (Sign in with Apple não é exigência de
 * política no Android, e o SDK nativo da Apple só funciona em iOS), e em web
 * mantém o botão preto customizado do fluxo #212.
 *
 * O estilo do botão nativo segue o mesmo racional de tema claro/escuro usado
 * no resto do app (ex: `Button`, `HorarioCard`): `BLACK` no tema claro e
 * `WHITE` no escuro — cada cor visível sobre o fundo do tema correspondente.
 *
 * O cancelamento do fluxo pelo usuário (`idToken === null`) não é erro nem
 * desfecho informado à tela: apenas não dispara nada, já que o Sign in with
 * Apple popup cancelado é esperado (ver `apple.ts`). Erro de negócio
 * (ex: e-mail não verificado) é exibido inline via `ErrorMessage`, mesmo
 * padrão dos formulários.
 */
export function BotaoLoginApple({ onAutenticado, onCadastroPendente }: BotaoLoginAppleProps) {
  const [carregando, setCarregando] = useState(false);
  const [erro, setErro] = useState<string | undefined>(undefined);
  const escuro = useColorScheme() === 'dark';
  // Ref (não state) porque `setCarregando` é assíncrono/batched: um segundo
  // toque síncrono, antes do primeiro `handlePress` re-renderizar, ainda leria
  // `carregando === false`. `AppleAuthenticationButton` (ramo iOS, #213) não
  // tem prop `disabled` como o `Pressable` do ramo web, então esta é a única
  // guarda contra duas chamadas concorrentes a
  // `obterIdTokenApple`/`loginComApple` disparando `onAutenticado`/
  // `onCadastroPendente` duas vezes.
  const emVooRef = useRef(false);

  async function handlePress() {
    if (emVooRef.current) {
      return;
    }
    emVooRef.current = true;
    setCarregando(true);
    setErro(undefined);

    const idToken = await obterIdTokenApple();
    if (!idToken) {
      emVooRef.current = false;
      setCarregando(false);
      return;
    }

    const resultado = await loginComApple(idToken);
    emVooRef.current = false;
    setCarregando(false);

    if (resultado.sucesso) {
      const { token, nome, papeis } = resultado;
      onAutenticado({ token, nome, papeis });
      return;
    }
    // `cadastroPendente` só existe no desfecho de e-mail sem conta — o `in`
    // discrimina a união de `LoginAppleResultado` (o acesso direto
    // `resultado.cadastroPendente` não compila porque o TS não garante a
    // presença da propriedade em todos os membros).
    if ('cadastroPendente' in resultado) {
      onCadastroPendente(resultado.email);
      return;
    }
    setErro(resultado.mensagem);
  }

  // Botão oficial do SDK da Apple — a diretriz da Apple exige o componente
  // próprio em apps nativos, não um botão customizado (diferente da web, onde
  // o HTML customizado seguindo a diretriz visual é aceito). Reutiliza o mesmo
  // `handlePress` do ramo web, que dispara `obterIdTokenApple` → `loginComApple`.
  if (Platform.OS === 'ios') {
    return (
      <>
        <AppleAuthentication.AppleAuthenticationButton
          buttonType={AppleAuthentication.AppleAuthenticationButtonType.SIGN_IN}
          buttonStyle={
            escuro
              ? AppleAuthentication.AppleAuthenticationButtonStyle.WHITE
              : AppleAuthentication.AppleAuthenticationButtonStyle.BLACK
          }
          onPress={handlePress}
          // Largura total como o botão web e altura mínima 44 (alvo de toque,
          // ver docs/spec/ux-heuristics.md#alvos-de-toque).
          style={{ width: '100%', minHeight: 44 }}
        />
        {erro ? <ErrorMessage>{erro}</ErrorMessage> : null}
      </>
    );
  }

  // Android: sem Sign in with Apple — o componente não renderiza nada.
  if (Platform.OS === 'android') {
    return null;
  }

  return (
    <>
      <Pressable
        accessibilityRole="button"
        onPress={handlePress}
        disabled={carregando}
        // Exceção documentada ao guardrail "sem fundo branco puro" e à
        // paleta de `theme/palette.js` — cor #000000 e texto branco exigidos
        // pela diretriz de marca da Apple para o botão "Sign in with Apple"
        // (issue #212).
        className={`w-full flex-row items-center justify-center gap-three bg-black px-four py-three ${
          carregando ? 'opacity-60' : ''
        }`}
        style={{ minHeight: 44 }}
      >
        <IconeApple />
        <Text className="text-base font-semibold text-white">
          {carregando ? 'Entrando...' : 'Continuar com Apple'}
        </Text>
      </Pressable>
      {erro ? <ErrorMessage>{erro}</ErrorMessage> : null}
    </>
  );
}
