﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Windows.Media.Animation;
using BiodiversityPlugin.Models;
using BiodiversityPlugin.Views;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using MessageBox = System.Windows.MessageBox;

namespace BiodiversityPlugin.ViewModels
{
    public class UpdateExistingViewModel : ViewModelBase
    {
        private string _blibPath;
        private List<string> _msgfPath;
        private string showMsgfPaths;
        private bool _nextEnable;
        private bool _previousEnable;
        private bool _startEnable;
        private bool _finishEnable;
        private bool _cancelEnable;
        private string _dbPath;
        private int _selectedTabIndex;
        private int _taskSelection; //0 is replace, 1 is supplement, 2 is insert new
        private string _selectedValue;
        private OrganismWithFlag _orgName;
        private ObservableCollection<OrganismWithFlag> _filteredOrganisms;
        private ObservableCollection<OrganismWithFlag> _allKeggOrgs;
        private ObservableCollection<OrganismWithFlag> _PBLOrganisms;
        private Visibility _filterVisibility;
        private bool _welcomeTabEnabled;
        private bool _inputTabEnabled;
        private bool _customizeTabEnabled;
        private bool _reviewTabEnabled;
        private string _displayMessage;
        private string _orgCode;


        public string DisplayMessage
        {
            get { return _displayMessage; }
            set
            {
                _displayMessage = value;
                RaisePropertyChanged();
            }
        }

        public bool WelcomeTabEnabled
        {
            get { return _welcomeTabEnabled; }
            set
            {
                _welcomeTabEnabled = value;
                RaisePropertyChanged();
            }
        }

        public bool InputTabEnabled
        {
            get { return _inputTabEnabled; }
            set
            {
                _inputTabEnabled = value;
                RaisePropertyChanged();
            }
        }

        public bool CustomizeTabEnabled
        {
            get { return _customizeTabEnabled; }
            set
            {
                _customizeTabEnabled = value;
                RaisePropertyChanged();
            }
        }

        public bool ReviewTabEnabled
        {
            get { return _reviewTabEnabled; }
            set
            {
                _reviewTabEnabled = value;
                RaisePropertyChanged();
            }
        }

        public Visibility FilterBoxVisible
        {
            get { return _filterVisibility; }
            set
            {
                _filterVisibility = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Property for the filter in the organism text box
        /// Whatever the filter is, this property will filter the list of organisms
        /// to fit the given criteria for the search filter
        /// </summary>
        public string SelectedValue
        {
            get { return _selectedValue; }
            set
            {
                _selectedValue = value;
                RaisePropertyChanged();

                //If task selection is already made, set the organism list that will be displayed
                //depending on which task they want to perform (different tasks have different lists of orgs)
                if (TaskSelection != null)
                {
                    if (TaskSelection == 2)
                    {
                        FilteredOrganisms = _allKeggOrgs;
                    }
                    else
                    {
                        FilteredOrganisms = _PBLOrganisms;
                    }
                }
                
                //Create a new simple list to add the orgs that match that criteria to
                var filtered = new List<string>();
                if (FilteredOrganisms != null)
                {
                    foreach (var org in FilteredOrganisms)
                    {
                        var name = org.OrganismName;
                        if (name.ToUpper().Contains(value.ToUpper()))
                        {
                            filtered.Add(name);
                        }
                    }
                }
                filtered.Sort();
                //Convert the filtered list to a collection with the flags
                FilteredOrganisms = new ObservableCollection<OrganismWithFlag>();
                FilteredOrganisms = OrganismWithFlag.ConvertToFlaggedList(filtered, _dbPath);

                FilterBoxVisible = Visibility.Hidden;
                if (FilteredOrganisms.Count > 0)
                {
                    FilterBoxVisible = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// Property to set the list of filtered organisms if the user is searching for
        /// and organism using the filter. And more letters are typed in
        /// the filter org list will get smaller and updates whenever new 
        /// criteria is added to the search box or when the clear filter button is pressed
        /// </summary>
        public ObservableCollection<OrganismWithFlag> FilteredOrganisms
        {
            get { return _filteredOrganisms; }
            set
            {
                _filteredOrganisms = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Property to set the collection of all the organisms in the
        /// PNNL biodiversity library. to be used with the replace and 
        /// supplement task selections
        /// </summary>
        public ObservableCollection<OrganismWithFlag> PBLOrganisms
        {
            get { return _PBLOrganisms;  }
            set
            {
                _PBLOrganisms = value;
                RaisePropertyChanged();
            }
        } 

        /// <summary>
        /// Property to set the collection of all the 
        /// organisms from kegg. To be used if the add new task is selected
        /// </summary>
        public ObservableCollection<OrganismWithFlag> AllKeggOrgs
        {
            get { return _allKeggOrgs; }
            set
            {
                _allKeggOrgs = value;
               RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Property to set the selected organism name 
        /// to perform customization on
        /// </summary>
        public OrganismWithFlag OrgName
        {
            get { return _orgName; }
            set
            {
                _orgName = value;
                RaisePropertyChanged();
                IsStartEnabled();
            }
        }

        /// <summary>
        /// Property to hold the current task selection
        /// options are replace = 0, supplement = 1 and add new = 2
        /// </summary>
        public int TaskSelection
        {
            get { return _taskSelection; }
            set
            {
                _taskSelection = value; 
                RaisePropertyChanged();
            }
        }

        public string DbPath
        {
            get { return _dbPath; }
            set { _dbPath = value; }
        }

        /// <summary>
        /// Property to get and set the blib location
        /// Have it check if next is enabled if they made a selection
        /// If both blib and msgf files are selected, regardless of order
        /// the next tab will be enabled
        /// </summary>
        public string BlibPath
        {
            get { return _blibPath; }
            set
            {
                _blibPath = value;
                RaisePropertyChanged();
                IsNextEnabled();
            }
        }

        /// <summary>
        /// Property to get and set the msgf path.
        /// Have it check if next is enabled if they made a selection
        /// </summary>
        public List<string> MsgfPath
        {
            get { return _msgfPath; }
            set
            {
                _msgfPath = value;
                RaisePropertyChanged();
                IsNextEnabled();
            }
        }

        /// <summary>
        /// Property that aids in the process of showing all the 
        /// pathways that are selected if there are more than one. Without this,
        /// it will not display correctly. (Combines all the names into one string.)
        /// </summary>
        public string ShowMsgfPaths
        {
            get { return showMsgfPaths; }
            set
            {
                showMsgfPaths = value; 
                RaisePropertyChanged();
            }
        }

        public bool NextButtonEnabled
        {
            get { return _nextEnable; }
            set
            {
                _nextEnable = value;
                RaisePropertyChanged();
            }
        }

        public bool PreviousButtonEnabled
        {
            get { return _previousEnable; }
            set
            {
                _previousEnable = value;
                RaisePropertyChanged();
            }
        }

        public bool StartButtonEnabled
        {
            get { return _startEnable; }
            set
            {
                _startEnable = value;
                RaisePropertyChanged();
            }
        }

        public bool FinishButtonEnabled
        {
            get { return _finishEnable; }
            set
            {
                _finishEnable = value;
                RaisePropertyChanged();
            }
        }

        public bool CancelButtonEnabled
        {
            get { return _cancelEnable; }
            set
            {
                _cancelEnable = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Property to check if the next button to start collecting the results at the end is enabled
        /// Requires the organism name to be selected and not null before continuing.
        /// </summary>
        private void IsStartEnabled()
        {
            if (OrgName != null)
            {
                if (!string.IsNullOrEmpty(OrgName.OrganismName))
                {
                    StartButtonEnabled = true;
                    RaisePropertyChanged();
                }
                else
                {
                    StartButtonEnabled = false;
                }
            }
            else
            {
                StartButtonEnabled = false;
            }
        }

        /// <summary>
        /// Property for the currently selected tab index.
        /// Constantly refreshes what tabs are allowed to be enabled
        /// and which ones are not for tab control navigation
        /// </summary>
        public int SelectedTabIndex
        {
            get { return _selectedTabIndex; }
            set
            {
                _selectedTabIndex = value;
                RaisePropertyChanged();
                if (SelectedTabIndex == 1)
                {
                    InputTabEnabled = true;
                }
                if (SelectedTabIndex == 2 && BlibPath != null && MsgfPath != null)
                {
                    StartButtonEnabled = false;
                    CustomizeTabEnabled = true;
                    ReviewTabEnabled = false;
                }
                if (SelectedTabIndex == 3)
                {
                    ReviewTabEnabled = true;
                }
                if (SelectedTabIndex == 4)
                {
                    CustomizeTabEnabled = false;
                    InputTabEnabled = false;
                    WelcomeTabEnabled = false;
                }
            }
        }

        public RelayCommand SelectBlibCommand { get; private set; }
        public RelayCommand SelectMsgfCommand { get; private set; }
        public RelayCommand HelpCommand { get; set; }
        public RelayCommand NextTabCommand { get; private set; }
        public RelayCommand PreviousTabCommand { get; private set; }
        public RelayCommand ReplaceSelected { get; private set; }
        public RelayCommand SupplementSelected { get; private set; }
        public RelayCommand AddNewSelected { get; private set; }
        public RelayCommand StartCommand { get; private set; }
        public RelayCommand FinishCommand { get; private set; }
        public RelayCommand CancelCommand { get; private set; }
        public RelayCommand ClearFilterCommand { get; private set; }

        public UpdateExistingViewModel(string dbpath, ObservableCollection<OrganismWithFlag> filteredOrganisms, List<string> allKeggOrgs )
        {
            _dbPath = dbpath;
            _PBLOrganisms = filteredOrganisms;
            FinishButtonEnabled = false;
            AllKeggOrgs = new ObservableCollection<OrganismWithFlag>();
            foreach (var org in allKeggOrgs)
            {             
                AllKeggOrgs.Add(new OrganismWithFlag(org, false));
            }
            SelectBlibCommand = new RelayCommand(SelectBlib);
            SelectMsgfCommand = new RelayCommand(SelectMsgf);
            HelpCommand = new RelayCommand(ClickHelp);
            NextTabCommand = new RelayCommand(NextTab);
            PreviousTabCommand = new RelayCommand(PreviousTab);
            _selectedTabIndex = 0;
            ReplaceSelected = new RelayCommand(SetReplace);
            SupplementSelected = new RelayCommand(SetSupplement);
            AddNewSelected = new RelayCommand(SetAddNew);
            StartCommand = new RelayCommand(Start);
            FinishCommand = new RelayCommand(Finish);
            CancelCommand = new RelayCommand(Cancel);
            ClearFilterCommand = new RelayCommand(ClearFilter);

            WelcomeTabEnabled = true;
            InputTabEnabled = false;
            CustomizeTabEnabled = false;
            ReviewTabEnabled = false;
        }

        private void ClearFilter()
        {
            SelectedValue = "";
            if (TaskSelection == 2)
            {
                FilteredOrganisms = _allKeggOrgs;
            }
            else
            {
                FilteredOrganisms = _PBLOrganisms;
            }
            FilterBoxVisible = Visibility.Visible;
        }

        private void NextTab()
        {
            SelectedTabIndex++;
        }

        private void ClickHelp()
        {
            var help = new HelpWindow();
            help.Show();
        }

        private void PreviousTab()
        {
            if (SelectedTabIndex > 0)
            {
                SelectedTabIndex--;
                if (SelectedTabIndex == 2)
                {
                    OrgName = null;
                    StartButtonEnabled = false;
                    WelcomeTabEnabled = true;
                    InputTabEnabled = true;
                    CustomizeTabEnabled = true;
                }
            }
        }

        private void IsNextEnabled()
        {
            if (!string.IsNullOrEmpty(BlibPath) && MsgfPath != null)
            {
                NextButtonEnabled = true;
                RaisePropertyChanged();
            }
            else
            {
                NextButtonEnabled = false;
            }
        }

        public void SetReplace()
        {
            ClearFilter();
            FilteredOrganisms = _PBLOrganisms;
            TaskSelection = 0;
        }

        public void SetSupplement()
        {
            ClearFilter();
            FilteredOrganisms = _PBLOrganisms;
            TaskSelection = 1;
        }

        public void SetAddNew()
        {
            ClearFilter();
            FilteredOrganisms = _allKeggOrgs;
            TaskSelection = 2;
        }

        private void Start()
        {
            //Take the user to the Review page
            NextTab();

            Task.Factory.StartNew(() =>
            {
                //Set all tabs to false while the process is running
                WelcomeTabEnabled = false;
                InputTabEnabled = false;
                CustomizeTabEnabled = false;
                PreviousButtonEnabled = false;
                CancelButtonEnabled = false;
                DisplayMessage = "Collecting results. Please wait, this could take a few minutes.";

                bool added = false;

                if (_taskSelection == 0)
                {
                    
                    _orgCode = UpdateExistingOrganism.GetKeggOrgCode(OrgName.OrganismName, DbPath);
                    DisplayMessage = UpdateExistingOrganism.UpdateExisting(OrgName.OrganismName, BlibPath, MsgfPath, DbPath, _orgCode);
                    PreviousButtonEnabled = true;
                    FinishButtonEnabled = true;
                    CancelButtonEnabled = true;
                }
                else if (_taskSelection == 1)
                {
                    
                    _orgCode = SupplementOrgansim.GetKeggOrgCode(OrgName.OrganismName, DbPath);
                    DisplayMessage = SupplementOrgansim.Supplement(OrgName.OrganismName, BlibPath, MsgfPath, DbPath, _orgCode);
                    PreviousButtonEnabled = true;
                    FinishButtonEnabled = true;
                    CancelButtonEnabled = true;
                }
                else if (_taskSelection == 2)
                {
                    bool alreadyAdded;                                    
                    DisplayMessage = InsertNewOrganism.InsertNew(_orgName.OrganismName, _blibPath, _msgfPath, _dbPath, out alreadyAdded);
                    added = alreadyAdded;
                    if (alreadyAdded == false)
                    {
                        PreviousButtonEnabled = true;
                        FinishButtonEnabled = true;
                        CancelButtonEnabled = true;
                    }
                    else
                    {
                        PreviousButtonEnabled = true;
                        CancelButtonEnabled = true;
                    }
                }
                //If statement to handle if a user adds an organism and it already exists. 
                //Default of added is false since user might not even add an organism
                if (added == false)
                {
                    DisplayMessage += "\n\n\nTo go back and make changes to the customizing options, click the Previous button." +
                                  " To confirm these changes, click the Finish button. To cancel, click the Cancel button.";
                }
                else
                {
                    DisplayMessage += "\n\n\nTo go back and make changes to the customizing options, click the Previous button." +
                                  " To cancel, click the Cancel button.";
                }
            });
        }

        private void Cancel()
        {
            this.CloseAction();
        }

        private void Finish()
        {
            if (TaskSelection == 2)
            {
                InsertNewOrganism.InsertIntoDb();
                InsertNewOrganism.UpdateBlibLocation(OrgName.OrganismName, BlibPath);
            }
            else if (TaskSelection == 1)
            {
                SupplementOrgansim.UpdateObservedKeggGeneTable(_orgCode);
                SupplementOrgansim.UpdateBlibLocation(OrgName.OrganismName, BlibPath);
            }
            else if (TaskSelection == 0)
            {
                UpdateExistingOrganism.UpdateObservedKeggGeneTable(_orgCode);
                UpdateExistingOrganism.UpdateBlibLocation(OrgName.OrganismName, BlibPath);
            }
            this.CloseAction();
        }

        public Action CloseAction { get; set; }

        private void SelectBlib()
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Filter = "Blib files (*.blib)|*.blib"; //.blib           
            var userClickedOK = openFile.ShowDialog();
            if (userClickedOK == DialogResult.OK)
            {
                BlibPath = openFile.FileName;
            }
        }

        private void SelectMsgf()
        {
            OpenFileDialog openFolder = new OpenFileDialog();
            openFolder.Multiselect = true;
            var userClickedOK = openFolder.ShowDialog();
            if (userClickedOK == DialogResult.OK)
            {
                var separator = ", ";
                var array = openFolder.FileNames;
                ShowMsgfPaths = string.Join(separator, array);
                MsgfPath = (openFolder.FileNames).ToList();
            }
        }
    }
}
