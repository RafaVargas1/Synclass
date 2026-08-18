import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { cadastrarProfessor, verificarContatoProfessor } from '@/lib/api/professores';

import CadastroProfessorScreen from './cadastro';

jest.mock('@/lib/api/professores', () => ({
  cadastrarProfessor: jest.fn(),
  verificarContatoProfessor: jest.fn(),
}));

const cadastrarProfessorMock = cadastrarProfessor as jest.Mock;
const verificarContatoProfessorMock = verificarContatoProfessor as jest.Mock;

describe('CadastroProfessorScreen', () => {
  beforeEach(() => {
    cadastrarProfessorMock.mockReset();
    verificarContatoProfessorMock.mockReset();
    verificarContatoProfessorMock.mockResolvedValue({ identidadeExistente: false, nome: null });
  });

  it('shows an inline confirmation when the Api responds with success', async () => {
    cadastrarProfessorMock.mockResolvedValue({ sucesso: true, nome: 'Maria Silva' });
    await render(<CadastroProfessorScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('Seu nome completo'), 'Maria Silva');
    await fireEvent.changeText(
      screen.getByPlaceholderText('E-mail ou telefone'),
      'maria@exemplo.com',
    );
    await fireEvent.press(screen.getByText('Cadastrar'));

    await waitFor(() => expect(screen.getByText('Cadastro concluído!')).toBeTruthy());
  });

  it('shows the Api error message without crashing when the Api rejects the cadastro', async () => {
    cadastrarProfessorMock.mockResolvedValue({
      sucesso: false,
      mensagem: 'O contato já está cadastrado como Professor.',
    });
    await render(<CadastroProfessorScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('Seu nome completo'), 'Maria Silva');
    await fireEvent.changeText(
      screen.getByPlaceholderText('E-mail ou telefone'),
      'maria@exemplo.com',
    );
    await fireEvent.press(screen.getByText('Cadastrar'));

    await waitFor(() =>
      expect(screen.getByText('O contato já está cadastrado como Professor.')).toBeTruthy(),
    );
    expect(screen.getByText('Cadastrar')).toBeTruthy();
  });

  it('locks the Nome field with the existing nome when the contato already has an identidade', async () => {
    verificarContatoProfessorMock.mockResolvedValue({
      identidadeExistente: true,
      nome: 'Maria Silva',
    });
    await render(<CadastroProfessorScreen />);

    const campoContato = screen.getByPlaceholderText('E-mail ou telefone');
    await fireEvent.changeText(campoContato, 'maria@exemplo.com');
    await fireEvent(campoContato, 'blur');

    await waitFor(() => expect(screen.getByDisplayValue('Maria Silva')).toBeTruthy());
    expect(screen.getByDisplayValue('Maria Silva').props.editable).toBe(false);
    expect(
      screen.getByText('Contato já cadastrado. Para corrigir o nome, edite pelo perfil depois de logado.'),
    ).toBeTruthy();
  });

  it('keeps the Nome field editable when the contato is new', async () => {
    await render(<CadastroProfessorScreen />);

    const campoContato = screen.getByPlaceholderText('E-mail ou telefone');
    await fireEvent.changeText(campoContato, 'novo@exemplo.com');
    await fireEvent(campoContato, 'blur');

    await waitFor(() => expect(verificarContatoProfessorMock).toHaveBeenCalledWith('novo@exemplo.com'));
    expect(screen.getByPlaceholderText('Seu nome completo').props.editable).not.toBe(false);
  });
});
