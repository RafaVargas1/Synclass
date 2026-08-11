import { fireEvent, render, screen } from '@testing-library/react-native';

import { Button } from '@/components/atoms/Button';

describe('Button', () => {
  it('renders the given label', async () => {
    await render(<Button label="Continuar" onPress={() => {}} />);

    expect(screen.getByText('Continuar')).toBeTruthy();
  });

  it('calls onPress when tapped', async () => {
    const onPress = jest.fn();
    await render(<Button label="Continuar" onPress={onPress} />);

    fireEvent.press(screen.getByText('Continuar'));

    expect(onPress).toHaveBeenCalledTimes(1);
  });
});
