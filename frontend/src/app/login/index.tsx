import { useLocalSearchParams, useRouter } from 'expo-router';
import { useState } from 'react';
import { View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { Button } from '@/components/atoms/Button';
import { Divisor } from '@/components/atoms/Divisor';
import { Paragraph } from '@/components/atoms/Paragraph';
import { BotaoLoginGoogle } from '@/components/molecules/BotaoLoginGoogle';
import { SolicitarCodigoForm } from '@/components/organisms/SolicitarCodigoForm';
import { Topbar } from '@/components/organisms/Topbar';
import { solicitarCodigo } from '@/lib/api/auth';
import { useSessao } from '@/lib/auth/contexto-sessao';
import { MaxContentWidth } from '@/theme/tokens';

/**
 * Tela de solicitação de login por código (issue #18): pede o contato e, em
 * caso de sucesso, navega para a tela de verificação (app/login/verificar.tsx)
 * levando o contato — só ela sabe para quem o código foi enviado.
 *
 * Também é a tela de entrada com conta Google (issue #65): o fluxo de
 * negócio fica no `BotaoLoginGoogle`, que devolve via callback o desfecho.
 * No sucesso persiste a sessão via `useSessao().definirSessao` e navega para
 * `/painel` (mesmo destino do OTP). Como o login não sabe o papel do
 * usuário, quando o e-mail ainda não tem conta o cadastro pendente mostra as
 * duas opções (Professor ou Aluno) — a mesma escolha que a Home já oferece —
 * levando o e-mail via `?email=` para pré-preenchimento do contato.
 */
export default function LoginScreen() {
  const router = useRouter();
  const { email } = useLocalSearchParams<{ email?: string }>();
  const { definirSessao } = useSessao();
  const [contato, setContato] = useState('');
  const [erro, setErro] = useState<string | undefined>(undefined);
  const [enviando, setEnviando] = useState(false);
  const [emailPendente, setEmailPendente] = useState<string | undefined>(email);

  async function handleSubmit() {
    setEnviando(true);
    setErro(undefined);

    const resultado = await solicitarCodigo({ contato });

    setEnviando(false);
    if (!resultado.sucesso) {
      setErro(resultado.mensagem);
      return;
    }
    router.push({ pathname: '/login/verificar', params: { contato } });
  }

  async function handleAutenticado({
    token,
    papeis,
  }: {
    token: string;
    papeis: string[];
  }) {
    try {
      await definirSessao(token, papeis);
      router.replace('/painel');
    } catch {
      setErro('Não foi possível concluir o login neste dispositivo. Tente novamente.');
    }
  }

  function handleCadastroPendente(email: string) {
    setEmailPendente(email);
  }

  function handleEscolhaCadastro() {
    // Remove as duas opções de cadastro pendente só depois de navegar — a
    // tela de cadastro destino é quem consome o e-mail via `?email=`.
    setEmailPendente(undefined);
  }

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <Topbar titulo="Entrar" />
      <View
        className="w-full flex-1 self-center items-center justify-center px-four"
        style={{ maxWidth: MaxContentWidth }}
      >
        {emailPendente ? (
          <View className="w-full gap-four">
            <Paragraph>
              Seu e-mail ainda não tem uma conta. Continue o cadastro como:
            </Paragraph>
            <Button
              label="Cadastrar como Professor"
              onPress={() => {
                handleEscolhaCadastro();
                router.push({
                  pathname: '/professor/cadastro',
                  params: { email: emailPendente },
                });
              }}
            />
            <Button
              label="Cadastrar como Aluno"
              onPress={() => {
                handleEscolhaCadastro();
                router.push({
                  pathname: '/aluno',
                  params: { email: emailPendente },
                });
              }}
            />
          </View>
        ) : (
          <View className="w-full gap-four">
            <SolicitarCodigoForm
              contato={contato}
              erro={erro}
              enviando={enviando}
              onChangeContato={setContato}
              onSubmit={handleSubmit}
            />
            <Divisor texto="ou" />
            <BotaoLoginGoogle
              onAutenticado={handleAutenticado}
              onCadastroPendente={handleCadastroPendente}
            />
          </View>
        )}
      </View>
    </SafeAreaView>
  );
}
