import { useState } from 'react';
import { View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { Heading } from '@/components/atoms/Heading';
import { Paragraph } from '@/components/atoms/Paragraph';
import { EntrarEmNovaTurmaForm } from '@/components/organisms/EntrarEmNovaTurmaForm';
import { TopbarAutenticada } from '@/components/organisms/TopbarAutenticada';
import { aceitarConvitePorCodigo } from '@/lib/api/convites';
import { useSessao } from '@/lib/auth/contexto-sessao';
import { usePerfilLogado } from '@/lib/usePerfilLogado';
import { MaxContentWidth } from '@/theme/tokens';

/**
 * Tela autenticada de entrada em nova turma por código de convite (issue
 * #144) — nome/contato do Aluno já logado (`usePerfilLogado`) são
 * enviados junto do código, sem pedir de novo (a Api casa por contato e
 * cria uma nova Matricula vinculada ao Professor do convite, ver
 * implementation.md desta issue). Diferente de `app/convite/[token].tsx`
 * (issue #2, fluxo público/não-autenticado com CadastroUsuarioForm) — este
 * fluxo é só pro Aluno que já tem conta.
 */
export default function EntrarEmNovaTurmaScreen() {
  const { token } = useSessao();
  const perfil = usePerfilLogado(token);
  const [codigo, setCodigo] = useState('');
  const [erro, setErro] = useState<string | undefined>(undefined);
  const [enviando, setEnviando] = useState(false);
  const [concluido, setConcluido] = useState(false);

  async function handleSubmit() {
    if (!perfil.nome || !perfil.contato) {
      setErro('Não foi possível confirmar seu perfil. Tente novamente em instantes.');
      return;
    }
    setEnviando(true);
    setErro(undefined);

    const resultado = await aceitarConvitePorCodigo({ codigo, nome: perfil.nome, contato: perfil.contato });

    setEnviando(false);
    if (!resultado.sucesso) {
      setErro(resultado.mensagem);
      return;
    }
    setConcluido(true);
  }

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <TopbarAutenticada titulo="Entrar em nova turma" />
      <View
        className="w-full flex-1 items-center justify-center self-center px-four"
        style={{ maxWidth: MaxContentWidth }}
      >
        {concluido ? (
          <View accessibilityRole="alert" className="items-center gap-two">
            <Heading level={1}>Turma adicionada!</Heading>
            <Paragraph>Você já pode ver os horários deste Professor nas suas telas de Aluno.</Paragraph>
          </View>
        ) : (
          <EntrarEmNovaTurmaForm
            codigo={codigo}
            erro={erro}
            enviando={enviando}
            onChangeCodigo={setCodigo}
            onSubmit={handleSubmit}
          />
        )}
      </View>
    </SafeAreaView>
  );
}
