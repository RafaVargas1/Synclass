import { fireEvent, render, screen } from '@testing-library/react-native';

import { Input } from '@/components/atoms/Input';

describe('Input', () => {
  it('renders the given value', async () => {
    await render(<Input value="Maria" onChangeText={() => {}} placeholder="Nome" />);

    expect(screen.getByDisplayValue('Maria')).toBeTruthy();
  });

  it('calls onChangeText when typed into', async () => {
    const onChangeText = jest.fn();
    await render(<Input value="" onChangeText={onChangeText} placeholder="Nome" />);

    fireEvent.changeText(screen.getByPlaceholderText('Nome'), 'Maria Silva');

    expect(onChangeText).toHaveBeenCalledWith('Maria Silva');
  });
});
