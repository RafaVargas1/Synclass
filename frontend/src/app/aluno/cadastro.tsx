import { useState } from 'react';
import { View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { CadastroConfirmado } from '@/components/molecules/CadastroConfirmado';
import { CadastroUsuarioForm } from '@/components/organisms/CadastroUsuarioForm';
import { Topbar } from '@/components/organisms/Topbar';
import { cadastrarAluno, verificarContatoAluno } from '@/lib/api/alunos';
import { MaxContentWidth } from '@/theme/tokens';

/**
 * Verifica, ao sair do campo Contato, se ele já pertence a uma identidade
 * existente (mesmo comportamento do cadastro de Professor, issue #27) —
 * trava o campo Nome com o valor já cadastrado nesse caso, já que a Api
 * descartaria silenciosamente um nome reenviado (RN da issue #20).
 * Separada de `CadastroAlunoScreen` só para caber no limite de 20 linhas
 * por função (`code-style.md`).
 */
function useVerificacaoDeContato(contato: string, setNome: (nome: string) => void) {
  const [nomeReadonly, setNomeReadonly] = useState(false);

  async function handleBlurContato() {
    const resultado = await verificarContatoAluno(contato);
    setNomeReadonly(resultado.identidadeExistente);
    if (resultado.identidadeExistente && resultado.nome) {
      setNome(resultado.nome);
    }
  }

  return { nomeReadonly, setNomeReadonly, handleBlurContato };
}

/**
 * Tela de cadastro independente de Aluno (issue #61): cria (ou reaproveita,
 * ver RN da issue) uma identidade de usuário com o papel Aluno, sem
 * nenhum vínculo com um Professor ainda — o vínculo nasce depois, no fluxo
 * de código de convite. Após sucesso, mostra uma confirmação inline — não
 * há área logada ainda para navegar (issue #18).
 */
export default function CadastroAlunoScreen() {
  const [nome, setNome] = useState('');
  const [contato, setContato] = useState('');
  const [erro, setErro] = useState<string | undefined>(undefined);
  const [enviando, setEnviando] = useState(false);
  const [concluido, setConcluido] = useState(false);
  const { nomeReadonly, setNomeReadonly, handleBlurContato } = useVerificacaoDeContato(contato, setNome);

  function handleChangeContato(contatoNovo: string) {
    setContato(contatoNovo);
    // Contato mudou depois de já ter passado pela verificação — o resultado
    // anterior (readonly com o nome de outra identidade) não vale mais até
    // o próximo blur confirmar de novo.
    setNomeReadonly(false);
  }

  async function handleSubmit() {
    setEnviando(true);
    setErro(undefined);

    const resultado = await cadastrarAluno({ nome, contato });

    setEnviando(false);
    if (!resultado.sucesso) {
      setErro(resultado.mensagem);
      return;
    }
    setConcluido(true);
  }

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <Topbar titulo="Cadastro de Aluno" />
      <View
        className="w-full flex-1 items-center justify-center self-center px-four"
        style={{ maxWidth: MaxContentWidth }}
      >
        {concluido ? (
          <CadastroConfirmado papel="Aluno" />
        ) : (
          <CadastroUsuarioForm
            nome={nome}
            contato={contato}
            erro={erro}
            enviando={enviando}
            nomeReadonly={nomeReadonly}
            onChangeNome={setNome}
            onChangeContato={handleChangeContato}
            onBlurContato={handleBlurContato}
            onSubmit={handleSubmit}
          />
        )}
      </View>
    </SafeAreaView>
  );
}
