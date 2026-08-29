import { useState } from 'react';
import { Pressable, Text } from 'react-native';

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
 * Molécula: botão único de entrada com conta Apple (issue #212),
 * reaproveitado nas mesmas telas que o `BotaoLoginGoogle` (Home, login,
 * cadastro Professor e cadastro Aluno). É a única que conhece o fluxo
 * completo da Apple (`obterIdTokenApple` → `loginComApple`), assim as telas
 * não duplicam a orquestração — a tela só decide o que fazer com o desfecho
 * via callback.
 *
 * Estilo próprio (diretriz de marca da Apple): botão preto com logo branco
 * e texto branco, na cor #000000 padrão "Sign in with Apple" — um botão de
 * terceiro que usa o estilo genérico do app quebra reconhecimento de marca
 * (mesma exceção documentada de `BotaoLoginGoogle.tsx`, ver
 * `docs/spec/ux-heuristics.md#reconhecimento-em-vez-de-recordação`).
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

  async function handlePress() {
    setCarregando(true);
    setErro(undefined);

    const idToken = await obterIdTokenApple();
    if (!idToken) {
      setCarregando(false);
      return;
    }

    const resultado = await loginComApple(idToken);
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
