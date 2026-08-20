import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { aceitarConvitePorCodigo } from '@/lib/api/convites';
import { cadastrarAluno, verificarContatoAluno } from '@/lib/api/alunos';

import AlunoScreen from './index';

jest.mock('@/lib/api/convites', () => ({
  aceitarConvitePorCodigo: jest.fn(),
}));

jest.mock('@/lib/api/alunos', () => ({
  cadastrarAluno: jest.fn(),
  verificarContatoAluno: jest.fn(),
}));

const aceitarConvitePorCodigoMock = aceitarConvitePorCodigo as jest.Mock;
const cadastrarAlunoMock = cadastrarAluno as jest.Mock;
const verificarContatoAlunoMock = verificarContatoAluno as jest.Mock;

describe('AlunoScreen', () => {
  beforeEach(() => {
    aceitarConvitePorCodigoMock.mockReset();
    cadastrarAlunoMock.mockReset();
    verificarContatoAlunoMock.mockReset();
    verificarContatoAlunoMock.mockResolvedValue({ identidadeExistente: false, nome: null });
  });

  it('mostra o campo de código e o formulário de nome/contato simultaneamente', async () => {
    await render(<AlunoScreen />);

    expect(screen.getByPlaceholderText('12345')).toBeTruthy();
    expect(screen.getByPlaceholderText('Seu nome completo')).toBeTruthy();
    expect(screen.getByPlaceholderText('E-mail ou telefone')).toBeTruthy();
    expect(screen.getByText('Código da turma (opcional)')).toBeTruthy();
  });

  it('chama aceitarConvitePorCodigo quando há código e mostra AceiteConviteConfirmado no sucesso', async () => {
    aceitarConvitePorCodigoMock.mockResolvedValue({
      sucesso: true,
      usuarioId: 'usuario-1',
      nome: 'João Pedro',
      papeis: ['Aluno'],
    });
    await render(<AlunoScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('12345'), '12345');
    await fireEvent.changeText(screen.getByPlaceholderText('Seu nome completo'), 'João Pedro');
    await fireEvent.changeText(screen.getByPlaceholderText('E-mail ou telefone'), '11987654321');
    await fireEvent.press(screen.getByText('Cadastrar'));

    await waitFor(() => expect(screen.getByText('Cadastro concluído!')).toBeTruthy());
    expect(screen.getByText(/João Pedro, seu cadastro foi concluído/)).toBeTruthy();
    expect(aceitarConvitePorCodigoMock).toHaveBeenCalledWith({
      codigo: '12345',
      nome: 'João Pedro',
      contato: '(11) 98765-4321',
    });
    expect(cadastrarAlunoMock).not.toHaveBeenCalled();
  });

  it('chama cadastrarAluno quando o código está vazio e mostra CadastroConfirmado com papel Aluno', async () => {
    cadastrarAlunoMock.mockResolvedValue({ sucesso: true, nome: 'João Souza' });
    await render(<AlunoScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('Seu nome completo'), 'João Souza');
    await fireEvent.changeText(
      screen.getByPlaceholderText('E-mail ou telefone'),
      'joao@exemplo.com',
    );
    await fireEvent.press(screen.getByText('Cadastrar'));

    await waitFor(() => expect(screen.getByText('Cadastro concluído!')).toBeTruthy());
    expect(screen.getByText('Seu cadastro como Aluno foi realizado com sucesso.')).toBeTruthy();
    expect(cadastrarAlunoMock).toHaveBeenCalledWith({
      nome: 'João Souza',
      contato: 'joao@exemplo.com',
    });
    expect(aceitarConvitePorCodigoMock).not.toHaveBeenCalled();
  });

  it('normaliza o código mascarado antes de enviar', async () => {
    aceitarConvitePorCodigoMock.mockResolvedValue({
      sucesso: true,
      usuarioId: 'usuario-1',
      nome: 'João Pedro',
      papeis: ['Aluno'],
    });
    await render(<AlunoScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('12345'), '1-2-3-4-5');
    await fireEvent.changeText(screen.getByPlaceholderText('Seu nome completo'), 'João Pedro');
    await fireEvent.changeText(screen.getByPlaceholderText('E-mail ou telefone'), '11987654321');
    await fireEvent.press(screen.getByText('Cadastrar'));

    await waitFor(() => expect(aceitarConvitePorCodigoMock).toHaveBeenCalled());
    expect(aceitarConvitePorCodigoMock).toHaveBeenCalledWith(
      expect.objectContaining({ codigo: '12345' }),
    );
  });

  it('mostra o estado dedicado de convite expirado quando o código expirou', async () => {
    aceitarConvitePorCodigoMock.mockResolvedValue({
      sucesso: false,
      mensagem: 'Este convite expirou. Peça ao Professor para gerar um novo link.',
    });
    await render(<AlunoScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('12345'), '12345');
    await fireEvent.changeText(screen.getByPlaceholderText('Seu nome completo'), 'João Pedro');
    await fireEvent.changeText(screen.getByPlaceholderText('E-mail ou telefone'), '11987654321');
    await fireEvent.press(screen.getByText('Cadastrar'));

    await waitFor(() => expect(screen.getByText('Convite expirado')).toBeTruthy());
    expect(screen.getByText('Peça ao Professor para gerar um novo link de convite.')).toBeTruthy();
  });

  it('mostra a mensagem de erro da Api inline sem crash e mantém o formulário editável', async () => {
    aceitarConvitePorCodigoMock.mockResolvedValue({
      sucesso: false,
      mensagem: 'Este Aluno já está vinculado a este Professor.',
    });
    await render(<AlunoScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('12345'), '12345');
    await fireEvent.changeText(screen.getByPlaceholderText('Seu nome completo'), 'João Pedro');
    await fireEvent.changeText(screen.getByPlaceholderText('E-mail ou telefone'), '11987654321');
    await fireEvent.press(screen.getByText('Cadastrar'));

    await waitFor(() =>
      expect(screen.getByText('Este Aluno já está vinculado a este Professor.')).toBeTruthy(),
    );
    expect(screen.getByPlaceholderText('Seu nome completo').props.editable).not.toBe(false);
    expect(screen.getByText('Cadastrar')).toBeTruthy();
  });

  it('mostra erro de contato inválido sem chamar a Api', async () => {
    await render(<AlunoScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('Seu nome completo'), 'João Pedro');
    await fireEvent.changeText(screen.getByPlaceholderText('E-mail ou telefone'), 'contato invalido');
    await fireEvent.press(screen.getByText('Cadastrar'));

    await waitFor(() =>
      expect(screen.getByText('Informe um e-mail ou telefone válido.')).toBeTruthy(),
    );
    expect(cadastrarAlunoMock).not.toHaveBeenCalled();
    expect(aceitarConvitePorCodigoMock).not.toHaveBeenCalled();
  });

  it('trava o campo Nome quando o contato pertence a uma identidade existente (verificarContatoAluno)', async () => {
    verificarContatoAlunoMock.mockResolvedValue({
      identidadeExistente: true,
      nome: 'João Souza',
    });
    await render(<AlunoScreen />);

    const campoContato = screen.getByPlaceholderText('E-mail ou telefone');
    await fireEvent.changeText(campoContato, 'joao@exemplo.com');
    await fireEvent(campoContato, 'blur');

    await waitFor(() => expect(screen.getByDisplayValue('João Souza')).toBeTruthy());
    expect(screen.getByDisplayValue('João Souza').props.editable).toBe(false);
  });

  it('sempre mostra o texto explicando que o vínculo com o Professor é feito depois', async () => {
    await render(<AlunoScreen />);

    expect(
      screen.getByText(
        /o vínculo com o Professor é feito depois, por código ou link/i,
      ),
    ).toBeTruthy();
  });
});
