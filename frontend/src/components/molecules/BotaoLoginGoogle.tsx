import { useState } from 'react';
import { Pressable, Text } from 'react-native';

import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { IconeGoogle } from '@/components/atoms/IconeGoogle';
import { loginComGoogle } from '@/lib/api/auth';
import { obterIdTokenGoogle } from '@/lib/auth/google';

export type ResultadoAutenticadoGoogle = {
  token: string;
  nome: string;
  papeis: string[];
};

export type BotaoLoginGoogleProps = {
  /**
   * Chamado quando o e-mail do idToken corresponde a um usuário existente —
   * carrega o resultado do login (token + papeis) para a tela definir a
   * sessão (ver `useSessao().definirSessao`).
   */
  onAutenticado: (resultado: ResultadoAutenticadoGoogle) => void;
  /**
   * Chamado quando o e-mail do idToken ainda não tem usuário correspondente
   * — carrega o e-mail normalizado para a tela levar o usuário ao cadastro.
   */
  onCadastroPendente: (email: string) => void;
};

/**
 * Molécula: botão único de entrada com conta Google (issue #65),
 * reaproveitado nas 4 telas que aceitam Google (Home, login, cadastro
 * Professor e cadastro Aluno). É a única que conhece o fluxo completo do
 * Google (`obterIdTokenGoogle` → `loginComGoogle`), assim as telas não
 * duplicam a orquestração — a tela só decide o que fazer com o desfecho via
 * callback.
 *
 * Estilo próprio (issue #113, não o átomo `Button` genérico do app): fundo
 * claro/borda + `IconeGoogle`, seguindo as diretrizes de marca do Google
 * pra botão "Sign in with Google" — um botão de terceiro que usa o estilo
 * genérico do app quebra reconhecimento de marca (ver
 * `docs/spec/ux-heuristics.md#reconhecimento-em-vez-de-recordação`).
 *
 * O cancelamento do fluxo pelo usuário (`idToken === null`) não é erro nem
 * desfecho informado à tela: apenas não dispara nada, já que o Google
 * Sign-In popup cancelado é esperado (ver `google.ts`). Erro de negócio
 * (ex: e-mail não verificado) é exibido inline via `ErrorMessage`, mesmo
 * padrão dos formulários.
 */
export function BotaoLoginGoogle({ onAutenticado, onCadastroPendente }: BotaoLoginGoogleProps) {
  const [carregando, setCarregando] = useState(false);
  const [erro, setErro] = useState<string | undefined>(undefined);

  async function handlePress() {
    setCarregando(true);
    setErro(undefined);

    const idToken = await obterIdTokenGoogle();
    if (!idToken) {
      setCarregando(false);
      return;
    }

    const resultado = await loginComGoogle(idToken);
    setCarregando(false);

    if (resultado.sucesso) {
      const { token, nome, papeis } = resultado;
      onAutenticado({ token, nome, papeis });
      return;
    }
    // `cadastroPendente` só existe no desfecho de e-mail sem conta — o `in`
    // discrimina a união de `LoginGoogleResultado` (o acesso direto
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
        // paleta de `theme/palette.js` (ver docs/spec/design-system.md#cor,
        // nota sobre BotaoLoginGoogle) — cores exatas (#747775, #8E918F,
        // #131314, #1E1F20, #1F1F1F, #E3E3E3, bg-white) exigidas pela
        // diretriz de marca do Google para o botão "Sign in with Google"
        // (issue #202).
        className={`w-full flex-row items-center justify-center gap-three border border-[#747775] bg-white px-four py-three active:bg-[#F8F9FA] dark:border-[#8E918F] dark:bg-[#131314] dark:active:bg-[#1E1F20] ${
          carregando ? 'opacity-60' : ''
        }`}
        style={{ minHeight: 44 }}
      >
        <IconeGoogle />
        <Text className="text-base font-semibold text-[#1F1F1F] dark:text-[#E3E3E3]">
          {carregando ? 'Entrando...' : 'Entrar com Google'}
        </Text>
      </Pressable>
      {erro ? <ErrorMessage>{erro}</ErrorMessage> : null}
    </>
  );
}
