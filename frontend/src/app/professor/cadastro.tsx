import { useState } from 'react';
import { View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { CadastroConfirmado } from '@/components/molecules/CadastroConfirmado';
import { CadastroProfessorForm } from '@/components/organisms/CadastroProfessorForm';
import { cadastrarProfessor } from '@/lib/api/professores';

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
            onChangeNome={setNome}
            onChangeContato={setContato}
            onSubmit={handleSubmit}
          />
        )}
      </View>
    </SafeAreaView>
  );
}
