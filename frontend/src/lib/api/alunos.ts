import { criarClienteCadastroUsuario } from './cadastroUsuario';

export type {
  CadastroUsuarioInput as CadastroAlunoInput,
  CadastroUsuarioResultado as CadastroAlunoResultado,
  VerificarContatoResultado,
} from './cadastroUsuario';

const cliente = criarClienteCadastroUsuario('/alunos');

export const cadastrarAluno = cliente.cadastrar;
export const verificarContatoAluno = cliente.verificarContato;
