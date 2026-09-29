using ShinroKensakuDesktop.ViewModels.Pages;
using System.Windows.Controls;

namespace ShinroKensakuDesktop.Views.Pages;

public partial class SearchPage : Page
{
    private bool loading;
    public SearchPage(SearchPageViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (loading) return;
            loading = true;
            try
            {
                await viewModel.LoadExamMethodComboBoxItems();
                await viewModel.LoadDepartmentComboBoxItems();
            }
            catch (Exception)
            {
                viewModel.StatusText = "検索条件を取得できませんでした。接続を確認し、画面を開き直してください。";
            }
            finally { loading = false; }
        };
    }
}
