import { criarClienteCadastroUsuario } from './cadastroUsuario';

export type {
  CadastroUsuarioInput as CadastroProfessorInput,
  CadastroUsuarioResultado as CadastroProfessorResultado,
  VerificarContatoResultado,
} from './cadastroUsuario';

const cliente = criarClienteCadastroUsuario('/professores');

export const cadastrarProfessor = cliente.cadastrar;
export const verificarContatoProfessor = cliente.verificarContato;
