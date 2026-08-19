import { useState } from 'react';
import { View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { AlunoProvisorioConfirmado } from '@/components/molecules/AlunoProvisorioConfirmado';
import { CadastroAlunoProvisorioForm } from '@/components/organisms/CadastroAlunoProvisorioForm';
import { Topbar } from '@/components/organisms/Topbar';
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
  const [identificador, setIdentificador] = useState('');
  const [erro, setErro] = useState<string | undefined>(undefined);
  const [enviando, setEnviando] = useState(false);
  const [nomeConfirmado, setNomeConfirmado] = useState<string | undefined>(undefined);

  async function handleSubmit() {
    setEnviando(true);
    setErro(undefined);

    const resultado = await cadastrarAlunoProvisorio({ nome, identificador });

    setEnviando(false);
    if (!resultado.sucesso) {
      setErro(resultado.mensagem);
      return;
    }
    setNomeConfirmado(resultado.nome);
  }

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <Topbar titulo="Cadastrar Aluno" />
      <View
        className="w-full flex-1 items-center justify-center self-center px-four"
        style={{ maxWidth: MaxContentWidth }}
      >
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
