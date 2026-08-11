import { useLocalSearchParams } from 'expo-router';
import { useState } from 'react';
import { View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { AlunoProvisorioConfirmado } from '@/components/molecules/AlunoProvisorioConfirmado';
import { CadastroAlunoProvisorioForm } from '@/components/organisms/CadastroAlunoProvisorioForm';
import { cadastrarAlunoProvisorio } from '@/lib/api/alunosProvisorios';

/**
 * Tela de cadastro de Aluno provisório (issue #3), aberta pelo Professor —
 * sem exigir nome de contato do Aluno, diferente do cadastro de Professor
 * (issue #1). `professorId` vem da rota (`[professorId]`), não de uma
 * sessão: login (issue #18) ainda não está mergeado. Ver decisão
 * documentada em docs/specs/3-aluno-provisorio/implementation.md.
 */
export default function CadastroAlunoProvisorioScreen() {
  const { professorId } = useLocalSearchParams<{ professorId: string }>();
  const [nome, setNome] = useState('');
  const [identificador, setIdentificador] = useState('');
  const [erro, setErro] = useState<string | undefined>(undefined);
  const [enviando, setEnviando] = useState(false);
  const [nomeConfirmado, setNomeConfirmado] = useState<string | undefined>(undefined);

  async function handleSubmit() {
    setEnviando(true);
    setErro(undefined);

    const resultado = await cadastrarAlunoProvisorio({ professorId, nome, identificador });

    setEnviando(false);
    if (!resultado.sucesso) {
      setErro(resultado.mensagem);
      return;
    }
    setNomeConfirmado(resultado.nome);
  }

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <View className="flex-1 items-center justify-center px-four">
        {nomeConfirmado ? (
          <AlunoProvisorioConfirmado nome={nomeConfirmado} />
        ) : (
          <CadastroAlunoProvisorioForm
            nome={nome}
            identificador={identificador}
            erro={erro}
            enviando={enviando}
            onChangeNome={setNome}
            onChangeIdentificador={setIdentificador}
            onSubmit={handleSubmit}
          />
        )}
      </View>
    </SafeAreaView>
  );
}
