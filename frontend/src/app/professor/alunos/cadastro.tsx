import { useState } from 'react';
import { View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { AlunoProvisorioConfirmado } from '@/components/molecules/AlunoProvisorioConfirmado';
import { CadastroAlunoProvisorioForm } from '@/components/organisms/CadastroAlunoProvisorioForm';
import { TopbarAutenticada } from '@/components/organisms/TopbarAutenticada';
import { cadastrarAlunoProvisorio } from '@/lib/api/alunosProvisorios';
import { MaxContentWidth } from '@/theme/tokens';

/**
 * Tela de cadastro de Aluno provisório (issue #3), aberta pelo Professor —
 * sem exigir nome de contato do Aluno, diferente do cadastro de Professor
 * (issue #1). Sem segmento dinâmico `[professorId]` na rota (issue #23): o
 * Professor é sempre quem está logado, a Api deriva a identidade da sessão
 * (token `Authorization`, anexado por `fetchComTimeout`) — não há mais
 * parâmetro para ler aqui. Ver decisão original documentada em
 * docs/specs/3-aluno-provisorio/implementation.md.
 */
export default function CadastroAlunoProvisorioScreen() {
  const [nome, setNome] = useState('');
  const [erro, setErro] = useState<string | undefined>(undefined);
  const [enviando, setEnviando] = useState(false);
  const [confirmado, setConfirmado] = useState<{ nome: string; identificador: string } | undefined>(
    undefined,
  );

  async function handleSubmit() {
    setEnviando(true);
    setErro(undefined);

    const resultado = await cadastrarAlunoProvisorio({ nome });

    setEnviando(false);
    if (!resultado.sucesso) {
      setErro(resultado.mensagem);
      return;
    }
    setConfirmado({ nome: resultado.nome, identificador: resultado.identificador });
  }

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <TopbarAutenticada titulo="Cadastrar Aluno" />
      <View
        className="w-full flex-1 items-center justify-center self-center px-four"
        style={{ maxWidth: MaxContentWidth }}
      >
        {confirmado ? (
          <AlunoProvisorioConfirmado nome={confirmado.nome} identificador={confirmado.identificador} />
        ) : (
          <CadastroAlunoProvisorioForm
            nome={nome}
            erro={erro}
            enviando={enviando}
            onChangeNome={setNome}
            onSubmit={handleSubmit}
          />
        )}
      </View>
    </SafeAreaView>
  );
}
