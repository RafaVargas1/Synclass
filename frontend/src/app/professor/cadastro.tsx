import { useState } from 'react';
import { View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { CadastroConfirmado } from '@/components/molecules/CadastroConfirmado';
import { CadastroProfessorForm } from '@/components/organisms/CadastroProfessorForm';
import { cadastrarProfessor, verificarContatoProfessor } from '@/lib/api/professores';

/**
 * Verifica, ao sair do campo Contato, se ele já pertence a uma identidade
 * existente (issue #27) — trava o campo Nome com o valor já cadastrado
 * nesse caso, já que a Api descartaria silenciosamente um nome reenviado
 * (RN da issue #20). Separada de `CadastroProfessorScreen` só para caber no
 * limite de 20 linhas por função (`code-style.md`).
 */
function useVerificacaoDeContato(contato: string, setNome: (nome: string) => void) {
  const [nomeReadonly, setNomeReadonly] = useState(false);

  async function handleBlurContato() {
    const resultado = await verificarContatoProfessor(contato);
    setNomeReadonly(resultado.identidadeExistente);
    if (resultado.identidadeExistente && resultado.nome) {
      setNome(resultado.nome);
    }
  }

  return { nomeReadonly, setNomeReadonly, handleBlurContato };
}

/**
 * Tela de cadastro de Professor (issue #1). Após sucesso, mostra uma
 * confirmação inline — não há área logada ainda para navegar (issue #18).
 */
export default function CadastroProfessorScreen() {
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

    const resultado = await cadastrarProfessor({ nome, contato });

    setEnviando(false);
    if (!resultado.sucesso) {
      setErro(resultado.mensagem);
      return;
    }
    setConcluido(true);
  }

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <View className="flex-1 items-center justify-center px-four">
        {concluido ? (
          <CadastroConfirmado />
        ) : (
          <CadastroProfessorForm
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
