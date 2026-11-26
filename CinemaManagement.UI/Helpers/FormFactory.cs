using Microsoft.Extensions.DependencyInjection;

namespace CinemaManagement.UI.Helpers;

public sealed class FormFactory
{
    private readonly IServiceProvider _provider;

    public FormFactory(IServiceProvider provider)
    {
        _provider = provider
                    ?? throw new ArgumentNullException(nameof(provider));
    }

    public T Create<T>() where T : Control
    {
        var form = _provider.GetRequiredService<T>();

        // Tự động áp dụng giao diện thống nhất.
        ThemeHelper.Apply(form);

        return form;
    }

    public DialogResult ShowDialog<T>(
        IWin32Window? owner = null)
        where T : Form
    {
        using var form = Create<T>();

        return owner is null
            ? form.ShowDialog()
            : form.ShowDialog(owner);
    }

    public void Show<T>(IWin32Window? owner = null)
        where T : Form
    {
        var form = Create<T>();

        form.FormClosed += (_, _) => form.Dispose();

        if (owner is null)
            form.Show();
        else
            form.Show(owner);
    }
}
