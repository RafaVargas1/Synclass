import { useState } from 'react';

import { Button } from '@/components/atoms/Button';
import { ErrorMessage } from '@/components/atoms/ErrorMessage';
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
 * reaproveitado nas 3 telas que aceitam Google (login, cadastro Professor e
 * cadastro Aluno). É a única que conhece o fluxo completo do Google
 * (`obterIdTokenGoogle` → `loginComGoogle`), assim as telas não duplicam a
 * orquestração — a tela só decide o que fazer com o desfecho via callback.
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
      <Button label={carregando ? 'Entrando...' : 'Entrar com Google'} onPress={handlePress} disabled={carregando} />
      {erro ? <ErrorMessage>{erro}</ErrorMessage> : null}
    </>
  );
}
