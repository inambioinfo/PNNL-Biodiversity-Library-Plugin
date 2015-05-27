﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using BiodiversityPlugin.Calculations;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using KeggDataLibrary.DataManagement;
using KeggDataLibrary.Models;
using SkylineTool;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;


namespace BiodiversityPlugin.ViewModels
{

    public class MainViewModel : ViewModelBase
    {
        #region Private attributes

        private readonly string _dbPath;

        private object _selectedOrganismTreeItem;
        private object _selectedPathwayTreeItem;

        private string _selectedOrganismText;
        private string _listPathwaySelectedItem;
        private string _selectedValue;

        private int _selectedTabIndex;
        private int _pathwayTabIndex;
        private int _pathwaysSelected;
        private List<string> _proteinsToExport;
        private List<string> _organismList;
        private Visibility _filterVisibility;

        private bool _listPathwaySelected;
        private bool _isOrganismSelected;
        private bool _isPathwaySelected;

        private bool _overviewTabEnabled;
        private bool _pathwaysTabEnabled;
        private bool _selectionTabEnabled;
        private bool _reviewTabEnabled;

        private ObservableCollection<Pathway> _selectedPathways;
        private ObservableCollection<string> _filteredOrganisms;
        private ObservableCollection<string> _listPathways;
        private ObservableCollection<OrganismPathwayProteinAssociation> _pathwayProteinAssociation;
        private ObservableCollection<ProteinInformation> _filteredProteins;

        private bool _isQuerying;
        private string _queryString;
        private string _priorOrg;
        private OrganismPathwayProteinAssociation _selectedAssociation;
        private string m_databaseVersion;
        private SkylineToolClient _toolClient;

        private Dictionary<string, string> _ncbiFastaDictionary;
        private List<string> _accessionsWithFastaErrors;
        private bool _ncbiDownloading;
        private string _pathwayCoverageOrg;
        private bool _versionBool;
        private int _topLevelWindow;
        private int _noNewDataCount;
        private Visibility _skylineSolution;
        private string _errorDetail;
        private string _errorMessage;
        private Visibility _ncbiSolution;
        private Visibility _massiveSolution;
        private bool _errorFound;
        private List<string> _parsedFiles = new List<string>(); 
        #endregion

        #region Public Properties

        public SkylineToolClient ToolClient
        {
            get { return _toolClient; }
            private set { _toolClient = value; }
        }

        /// <summary>
        /// Property to display the current Skyline version of the user to the failure page.
        /// </summary>
        public string TextVersionMessage
        {
            get
            {
                if (ToolClient != null)
                {
                    var version = ToolClient.GetSkylineVersion();
                    return string.Format("Current version of Skyline: {0}.{1}.{2}.{3}", version.Major, version.Minor,
                        version.Build, version.Revision);
                }
                return "";
            }
        }

        public string DatabaseDate { get; set; }

        /// <summary>
        /// Property to display the version ofthe BioDiversity Library's DB that the user has installed
        /// </summary>
        public string DatabaseVersion
        {
            get
            {
                return "Biodiversity Library v" + m_databaseVersion;
            }
            set
            {
                m_databaseVersion = value;
            }
        }

        public ObservableCollection<OrgDomain> Organisms { get; private set; }
        public ObservableCollection<PathwayCatagory> Pathways { get; private set; }

        public Organism SelectedOrganism { get; private set; }
        public Pathway SelectedPathway { get; private set; }

        public OrganismPathwayProteinAssociation SelectedAssociation
        {
            get
            {
                return _selectedAssociation;
            }
            set
            {
                if (value != null)
                {
                    _selectedAssociation = value;
                    var temp = value.AssociationSelected;
                    _selectedAssociation.AssociationSelected = temp == false;
                    RaisePropertyChanged();
                }
                else
                {
                    _selectedAssociation.AssociationSelected = _selectedAssociation.AssociationSelected == false;
                }
            }
        }

        public ObservableCollection<Pathway> SelectedPathways
        {
            get { return _selectedPathways; }
            set
            {
                _selectedPathways = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<ProteinInformation> FilteredProteins
        {
            get
            {
                return _filteredProteins;
            }
            private set
            {
                _filteredProteins = value;
                RaisePropertyChanged();
            }
        }

        public string SelectedOrganismText
        {
            get { return "3) Curate protein list for " + _selectedOrganismText; }
            private set
            {
                _selectedOrganismText = value;
                IsOrganismSelected = true;
                RaisePropertyChanged();
            }
        }

        public bool IsOrganismSelected
        {
            get { return _isOrganismSelected; }
            set
            {
                _isOrganismSelected = value;
                RaisePropertyChanged();
            }
        }

        public bool IsPathwaySelected
        {
            get { return _isPathwaySelected; }
            set
            {
                _isPathwaySelected = value;
                RaisePropertyChanged();
            }
        }

        public object SelectedOrganismTreeItem
        {
            get { return _selectedOrganismTreeItem; }
            set
            {
                var orgValue = value as Organism;
                if (orgValue != null)
                {
                    _selectedOrganismTreeItem = value;
                    SelectedOrganism = (Organism)_selectedOrganismTreeItem;
                    IsOrganismSelected = false;
                    if (SelectedOrganism != null)
                        SelectedOrganismText = string.Format("Organism: {0}", SelectedOrganism.Name);
                    RaisePropertyChanged();
                }
            }
        }

        public object SelectedPathwayTreeItem
        {
            get { return _selectedPathwayTreeItem; }
            set
            {
                _selectedPathwayTreeItem = value;
                SelectedPathway = _selectedPathwayTreeItem as Pathway;
                IsPathwaySelected = false;
                if (SelectedPathway != null)
                {
                    SelectedPathway.Selected = SelectedPathway.Selected == false;
                }
                RaisePropertyChanged();
            }
        }

        public List<string> ProteinsToExport
        {
            get { return _proteinsToExport; }
            private set
            {
                _proteinsToExport = value;
                RaisePropertyChanged();
            }
        }

        public bool IsQuerying
        {
            get { return _isQuerying; }
            private set
            {
                _isQuerying = value;
                RaisePropertyChanged();
            }
        }

        public string QueryString
        {
            get { return _queryString; }
            private set
            {
                _queryString = value;
                RaisePropertyChanged();
            }
        }

        public List<string> OrganismList
        {
            get { return _organismList; }
            set
            {
                _organismList = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Property for the filter in the Organism text box
        /// </summary>
        public string SelectedValue
        {
            get { return _selectedValue; }
            set
            {
                _selectedValue = value;
                RaisePropertyChanged();
                var filtered = (from domain in Organisms
                                from phylum in domain.OrgPhyla
                                from orgClass in phylum.OrgClasses
                                from organism in orgClass.Organisms
                                where organism.Name.ToUpper().Contains(value.ToUpper())
                                select organism.Name).ToList();
                filtered.Sort();
                FilteredOrganisms = new ObservableCollection<string>(filtered);
                FilterBoxVisible = Visibility.Hidden;
                if (FilteredOrganisms.Count > 0)
                {
                    FilterBoxVisible = Visibility.Visible;
                }
                SelectedOrganismTreeItem = null;
            }
        }

        public ObservableCollection<OrganismPathwayProteinAssociation> PathwayProteinAssociation
        {
            get { return _pathwayProteinAssociation; }
            set
            {
                _pathwayProteinAssociation = value;
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

        public ObservableCollection<String> FilteredOrganisms
        {
            get { return _filteredOrganisms; }
            set
            {
                _filteredOrganisms = value;
                RaisePropertyChanged();
            }
        }

        public string SelectedListOrg
        {
            get { return "Selected"; }
            set
            {
                foreach (var domain in Organisms)
                {

                    foreach (var phylum in domain.OrgPhyla)
                    {
                        foreach (var orgClass in phylum.OrgClasses)
                        {
                            foreach (var organism in orgClass.Organisms)
                            {
                                if (organism.Name == value)
                                {
                                    SelectedOrganismTreeItem = organism;
                                    // To refresh the Pathway tab's ability to be
                                    // clicked by the user to advance the app.
                                    SelectedTabIndex = SelectedTabIndex;
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }

        public ObservableCollection<string> ListPathways
        {
            get { return _listPathways; }
            set
            {
                _listPathways = value;
                // To refresh the Selection tab's ability to be
                // clicked by the user to advance the app.
                SelectedTabIndex = SelectedTabIndex;
                RaisePropertyChanged();
            }
        }

        public string ListPathwaySelectedItem
        {
            get { return _listPathwaySelectedItem; }
            set
            {
                _listPathwaySelectedItem = value;
                
                ListPathwaySelected = ListPathways.Contains(value);
                RaisePropertyChanged();
            }
        }

        public bool ListPathwaySelected
        {
            get { return _listPathwaySelected; }
            set
            {
                _listPathwaySelected = value;
                RaisePropertyChanged();
            }
        }

        public bool PathwaysTabEnabled
        {
            get { return _pathwaysTabEnabled; }
            set
            {
                _pathwaysTabEnabled = value;
                RaisePropertyChanged();
            }
        }

        public bool SelectionTabEnabled
        {
            get { return _selectionTabEnabled; }
            set
            {
                _selectionTabEnabled = value;
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

        /// <summary>
        /// Only used when all tabs are disabled (during loading)
        /// True means Organism AND Overview tabs are enabled
        /// </summary>
        public bool OverviewEnabled
        {
            get { return _overviewTabEnabled; }
            set
            {
                _overviewTabEnabled = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Commands

        public RelayCommand NextTabCommand { get; private set; }
        public RelayCommand PreviousTabCommand { get; private set; }
        public RelayCommand AcquireProteinsCommand { get; private set; }
        public RelayCommand ExportToSkylineCommand { get; private set; }
        public RelayCommand DisplayPathwayImagesCommand { get; private set; }
        public RelayCommand SelectAdditionalOrganismCommand { get; private set; }
        public RelayCommand DeleteSelectedPathwayCommand { get; private set; }
        public RelayCommand SelectPathwayCommand { get; private set; }
        public RelayCommand ClearFilterCommand { get; private set; }
        public RelayCommand LoadPathwayCoverageCommand { get; private set; }
        public RelayCommand CloseAppCommand { get; private set; }

        #endregion

        #region TabIndexes

        /// <summary>
        /// Property for the inner tab control where pathways are housed during
        /// selection of proteins
        /// </summary>
        public int PathwayTabIndex
        {
            get { return _pathwayTabIndex; }
            set { _pathwayTabIndex = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// Property for the Selected Tab Index.
        /// This also constantly refreshes what tabs are enabled and which ones
        /// are not for tab control navigation
        /// </summary>
        public int SelectedTabIndex
        {
            get { return _selectedTabIndex; }
            set
            {
                _selectedTabIndex = value;
                PathwaysTabEnabled = false;
                SelectionTabEnabled = false;
                ReviewTabEnabled = false;
                RaisePropertyChanged();
                if (SelectedTabIndex > 2 || SelectedOrganism != null)
                {
                    PathwaysTabEnabled = true;
                }
                if (SelectedTabIndex > 3 || ListPathways.Count != 0)
                {
                    SelectionTabEnabled = true;
                }
                if (SelectedTabIndex == 3)
                {
                    ReviewTabEnabled = true;
                }
            }
        }

        /// <summary>
        /// Property for the top level window index
        /// 0 - Main view, the place the users spend the majority of their time
        /// 1 - Successful import view, to inform the user their data is in
        /// 2 - Error/failure view, primarily used to inform the user their skyline version is out of date
        /// </summary>
        public int TopLevelWindow
        {
            get { return _topLevelWindow; }
            set
            {
                _topLevelWindow = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        /// <summary>
        /// Constructor for the view model
        /// </summary>
        /// <param name="orgData">Database connection which contains the organism data</param>
        /// <param name="pathData">Database connection which contains the pathway data</param>
        /// <param name="dbPath">Database location which contains the kegg and ncbi data</param>
        /// <param name="toolClient">Skyline tool connection, is null if application ran in standalone mode</param>
        /// <param name="goodVersion">Boolean flag for if the user's skyline version is late enough to allow the data to be pushed back to Skyline</param>
        public MainViewModel(IDataAccess orgData, IDataAccess pathData, string dbPath, ref SkylineToolClient toolClient, bool goodVersion)
        {

            _dbPath = dbPath;
            ToolClient = toolClient;
            var dataAccess = new DatabaseDataLoader(_dbPath);
            string version, date;
            dataAccess.LoadDbMetaData(out version, out date);
            DatabaseVersion = version;
            DatabaseDate = date;

            Messenger.Default.Register<PropertyChangedMessage<bool>>(this, PathwaysSelectedChanged);
            var organismList = new List<string>();
            var organisms = orgData.LoadOrganisms(ref organismList);

            organismList.Sort();
            OrganismList = organismList;
            organisms.Sort((x, y) => x.DomainName.CompareTo(y.DomainName));
            Organisms = new ObservableCollection<OrgDomain>(organisms);
            Pathways = new ObservableCollection<PathwayCatagory>(pathData.LoadPathways());

            FilteredProteins = new ObservableCollection<ProteinInformation>();
            PreviousTabCommand = new RelayCommand(PreviousTab);
            NextTabCommand = new RelayCommand(NextTab);
            AcquireProteinsCommand = new RelayCommand(AcquireProteins);
            ExportToSkylineCommand = new RelayCommand(ExportToSkyline);
            DisplayPathwayImagesCommand = new RelayCommand(DisplayPathwayImages);
            SelectAdditionalOrganismCommand = new RelayCommand(SelectAdditionalOrganism);
            DeleteSelectedPathwayCommand = new RelayCommand(DeleteSelectedPathway);
            SelectPathwayCommand = new RelayCommand(SelectPathway);
            ClearFilterCommand = new RelayCommand(ClearFilter);
            LoadPathwayCoverageCommand = new RelayCommand(LoadPathwayCoverage);
            CloseAppCommand = new RelayCommand(CloseApplication);

            _pathwayTabIndex = 0;
            _selectedTabIndex = 0;
            TopLevelWindow = 0;
            _isOrganismSelected = false;
            _isPathwaySelected = false;

            _ncbiFastaDictionary = new Dictionary<string, string>();
            _accessionsWithFastaErrors = new List<string>();
            _pathwaysSelected = 0;
            ListPathways = new ObservableCollection<string>();

            _selectedPathways = new ObservableCollection<Pathway>();
            SelectedPathways = _selectedPathways;

            ProteinsToExport = new List<string>();
            PathwayProteinAssociation = new ObservableCollection<OrganismPathwayProteinAssociation>();
            SelectedValue = "";
            _priorOrg = "";
            _pathwayCoverageOrg = "";
            _overviewTabEnabled = true;
            _noNewDataCount = 0;
            ErrorDetail = "";
            ErrorMessage = "";
            SkylineSolution = Visibility.Collapsed;
            NcbiSolution = Visibility.Collapsed;
            MassiveSolution = Visibility.Collapsed;
            _errorFound = false;

            if (ToolClient != null && !goodVersion)
            {
                ErrorMessage = "ERROR: Your Skyline version must be version 3.1.1.7490 or later.";
                ErrorDetail = TextVersionMessage;
                SkylineSolution = Visibility.Visible;
                _errorFound = true;
                TopLevelWindow = 2;
            }

        }

        /// <summary>
        /// Used for buttons which can close the application (Currently only in the success and failure pages)
        /// </summary>
        private void CloseApplication()
        {
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Used for button to clear the filter
        /// </summary>
        private void ClearFilter()
        {
            SelectedValue = "";
        }

        /// <summary>
        /// Method which dynamically loads the pathway coverage in a new thread
        /// that only processes data while the user is in the Pathway View tab
        /// and selecting the pathways of interest
        /// </summary>
        private void LoadPathwayCoverage()
        {
            if (SelectedTabIndex == 1 && SelectedOrganism == null) return;
            SelectedTabIndex = 2;

            if (_pathwayCoverageOrg != SelectedOrganism.Name)
            {
                _pathwayCoverageOrg = SelectedOrganism.Name;
                foreach (var catagory in Pathways)
                {
                    foreach (var g in catagory.PathwayGroups)
                    {
                        foreach (var pathway in g.Pathways)
                        {
                            pathway.PercentCover = -1;
                        }
                    }
                }
            }

            var coordPrefix = _dbPath.Replace("DataFiles\\PBL.db", "");

            Task.Factory.StartNew(() =>
            {
                var dataAccess = new DatabaseDataLoader(_dbPath);

                var pathList = (from catagory in Pathways 
                                from @group in catagory.PathwayGroups 
                                where @group.GroupName != "Chemical structure transformation maps" 
                                from path in @group.Pathways select path).ToList();
                
                foreach (var path in pathList)
                {
                    if (SelectedTabIndex == 2 && path.PercentCover <= -1)
                    {
                        var pathAsList = new List<Pathway> {path};
                        dataAccess.LoadPathwayCoverage(SelectedOrganism, ref pathAsList, coordPrefix);
                        foreach (var pathway in from catagory in Pathways
                            from @group in catagory.PathwayGroups
                            from pathway in @group.Pathways
                            where pathway.KeggId == pathAsList[0].KeggId
                            select pathway)
                        {
                            pathway.PercentCover = path.PercentCover;
                        }
                    }
                }

            });
        }

        /// <summary>
        /// Event handler to update the Pathways that a user has selected
        /// </summary>
        /// <param name="message">The pathway that a user freshly selected</param>
        private void PathwaysSelectedChanged(PropertyChangedMessage<bool> message)
        {
            if (message.PropertyName == "Selected" && message.Sender is Pathway)
            {
                var old = ListPathways;
                var sender = message.Sender as Pathway;
                if (message.NewValue == true)
                {
                    if (!old.Contains(sender.Name))
                    {
                        old.Add(sender.Name);
                    }
                    ListPathways = old;
                    _pathwaysSelected++;
                    IsPathwaySelected = true;
                }
                else
                {
                    old.Remove(sender.Name);
                    ListPathways = old;
                    _pathwaysSelected--;

                    if (_pathwaysSelected == 0)
                    {
                        IsPathwaySelected = false;
                    }
                }
            }
        }

        /// <summary>
        /// Method to select new Pathways
        /// </summary>
        private void SelectPathway()
        {
            var temp = SelectedPathwayTreeItem;
            SelectedPathwayTreeItem = temp;
        }

        /// <summary>
        /// Method to delete a pathway from the list of selected pathways
        /// and update the view on the GUI
        /// </summary>
        private void DeleteSelectedPathway()
        {
            var treePathway = SelectedPathwayTreeItem as Pathway;
            if (ListPathwaySelectedItem != null)
            {
                foreach (var pathway in
                            from pathwayCatagory in Pathways
                            from pathwayGroup in pathwayCatagory.PathwayGroups
                            from pathway in pathwayGroup.Pathways
                            where ListPathwaySelectedItem == pathway.Name
                            select pathway)
                {
                    pathway.Selected = false;
                    var temp = ListPathways;
                    temp.Remove(ListPathwaySelectedItem);
                    ListPathways = temp;
                }
            }
            else if (treePathway != null)
            {
                foreach (var pathwayCatagory in Pathways)
                {
                    foreach (var pathwayGroup in pathwayCatagory.PathwayGroups)
                    {
                        foreach (var pathway in pathwayGroup.Pathways)
                        {
                            if (treePathway.Name == pathway.Name)
                            {
                                pathway.Selected = false;
                                var temp = ListPathways;
                                temp.Remove(treePathway.Name);
                                ListPathways = temp;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Method to tab backwards through the GUI
        /// </summary>
        private void PreviousTab()
        {
            if (SelectedTabIndex > 0)
            {
                SelectedTabIndex--;
            }
            // If the user is now on Tab Index 2, continue the calculation of pathway coverage
            if (SelectedTabIndex == 2)
            {
                LoadPathwayCoverage();
            }
        }

        /// <summary>
        /// Method to tab forward through the application, including controlling
        /// whether the user can move forward or not.
        /// </summary>
        private void NextTab()
        {
            // Do nothing if no selected organism and on the organism selection tab
            if (SelectedTabIndex == 1 && SelectedOrganism == null) return;
            // Do nothing if no selected pathway and on the pathway selection tab
            if (SelectedTabIndex == 2 && !IsPathwaySelected) return;
            SelectedTabIndex++;
        }

        /// <summary>
        /// Method to display the images for the pathways with whether we have data observed in MSMS space
        /// </summary>
        private void DisplayPathwayImages()
        {
            // Advance to the next tab
            SelectedTabIndex = 3;

            string[] queryingStrings =
			    {
				    "Generating Pathway Images   \nPlease Wait",
				    "Generating Pathway Images.  \nPlease Wait",
				    "Generating Pathway Images.. \nPlease Wait",
				    "Generating Pathway Images...\nPlease Wait"
			    };
            QueryString = queryingStrings[0];

            var dataAccess = new DatabaseDataLoader(_dbPath);
            var coordPrefix = _dbPath.Replace("DataFiles\\PBL.db", "");
            var currentOrg = SelectedOrganism.Name;
            var curPathways = new ObservableCollection<Pathway>((from pathwayCatagory in Pathways
                                                                 from @group in pathwayCatagory.PathwayGroups
                                                                 from p in @group.Pathways
                                                                 where p.Selected
                                                                 select p).ToList());

            // Check if the current pathways selected are the same as the prior selected pathways
            // If they are and the org is the same, nothing needs to be done to display images.
            var same = !(curPathways.Count != SelectedPathways.Count ||
                        (curPathways.Any(pathway => !SelectedPathways.Contains(pathway)) ||
                            SelectedPathways.Any(pathway => !curPathways.Contains(pathway))));

            // Need this for when anything in the canvas changes.
            // The application level dispatcher needs to be utilized and through
            // dis.Invoke(() => <COMMAND TO EXECUTE> );
            var dis = Application.Current.Dispatcher;

            // Start the animated overlay with the message set above
            Task.Factory.StartNew(() => StartOverlay(queryingStrings));

            //Task.Factory.StartNew(() => StartFastaDownloads(SelectedOrganism, curPathways.ToList()));
            Task.Factory.StartNew(() => StartFastaDownloads(SelectedOrganism));

            Task.Factory.StartNew((() =>
            {
                if (currentOrg != _priorOrg || !same)
                {
                    var selectedPaths = new List<Pathway>();
                    foreach (var catagory in Pathways)
                    {
                        foreach (var group in catagory.PathwayGroups)
                        {
                            foreach (var pathway in group.Pathways)
                            {
                                if (pathway.Selected && !_errorFound)
                                {
                                    // Load the image (From the static location)
                                    dis.Invoke(() =>
                                        pathway.LoadImage(coordPrefix));

                                    // Remove any rectangles from the canvas to provide accurate visualization
                                    dis.Invoke(() =>
                                        pathway.ClearRectangles());

                                    // Draw the information for the Legend on each image.
                                    var legendText = "Protein annotated in " + SelectedOrganism.Name +
                                                     " and observed in MS/MS data";
                                    dis.Invoke(() =>
                                        pathway.DrawPositiveLegend(10, 5, legendText, Colors.Red));

                                    legendText = "Protein annotated in " + SelectedOrganism.Name +
                                                 " and not observed in MS/MS data";
                                    dis.Invoke(() =>
                                        pathway.DrawNegativeLegend(10, 22, legendText, Colors.Blue));

                                    // Now that we have the base image and the legend, load the coordinates
                                    // for every rectangle on the image, keyed on KO name.
                                    var koToCoordDict = pathway.LoadCoordinates(coordPrefix);

                                    // Use the database to determine which orthologs have data in MSMS and load
                                    // the coordinates
                                    var koWithData = dataAccess.ExportKosWithData(pathway, SelectedOrganism);
                                    var coordToName = new Dictionary<Tuple<int, int>, List<KeggKoInformation>>();
                                    foreach (var ko in koWithData)
                                    {
                                        if (koToCoordDict.ContainsKey(ko.KeggKoId))
                                        {
                                            foreach (var coord in koToCoordDict[ko.KeggKoId])
                                            {
                                                if (!coordToName.ContainsKey(coord))
                                                {

                                                    coordToName[coord] = new List<KeggKoInformation>();
                                                }
                                                coordToName[coord].Add(ko);
                                            }
                                        }
                                    }
                                    foreach (var coord in coordToName)
                                    {
                                        var koInformation = coord.Value;
                                        var koIds = koInformation.First().KeggKoId;
                                        var keggGeneNames = koInformation.First().KeggGeneName;
                                        var keggEcs = koInformation.First().KeggEc;

                                        foreach (var ko in koInformation)
                                            if (ko != koInformation.First())
                                            {
                                                koIds += ", " + ko.KeggKoId;
                                                keggGeneNames += ", " + ko.KeggGeneName;
                                                keggEcs += ", " + ko.KeggEc;
                                            }

                                        var tooltip = string.Format("{0}\nGene Name: {1}\nKegg Ec: {2}", koIds,
                                            keggGeneNames, keggEcs);

                                        // Draw data rectangles for each of these coordinates
                                        // These rectangles are able to be interacted with by the user
                                        dis.Invoke(() => pathway.AddRectangle(coord.Key.Item1,
                                            coord.Key.Item2, true, 0.5, tooltip, koIds));
                                    }

                                    // Do the same for orthologs without data in MSMS, loading the coordinates needed
                                    var koWithoutData = dataAccess.ExportKosWithoutData(pathway, SelectedOrganism);

                                    var coordsToName = new Dictionary<Tuple<int, int>, List<KeggKoInformation>>();
                                    foreach (var ko in koWithoutData)
                                    {
                                        if (koToCoordDict.ContainsKey(ko.KeggKoId))
                                        {
                                            foreach (var coord in koToCoordDict[ko.KeggKoId])
                                            {
                                                if (!coordToName.ContainsKey(coord))
                                                {
                                                    if (!coordsToName.ContainsKey(coord))
                                                    {

                                                        coordsToName[coord] = new List<KeggKoInformation>();
                                                    }
                                                    coordsToName[coord].Add(ko);
                                                }
                                            }
                                        }
                                    }
                                    foreach (var coord in coordsToName)
                                    {
                                        var koInformation = coord.Value;
                                        var koIds = koInformation.First().KeggKoId;
                                        var keggGeneNames = koInformation.First().KeggGeneName;
                                        var keggEcs = koInformation.First().KeggEc;

                                        foreach (var ko in koInformation)
                                            if (ko != koInformation.First())
                                            {
                                                koIds += ", " + ko.KeggKoId;
                                                keggGeneNames += ", " + ko.KeggGeneName;
                                                keggEcs += ", " + ko.KeggEc;
                                            }

                                        var tooltip = string.Format("{0}\nGene Name: {1}\nKegg Ec: {2}", koIds,
                                            keggGeneNames, keggEcs);

                                        // Draw non-data rectangles for each of these coordinates
                                        // These rectangles have no interaction from the user
                                        dis.Invoke(() => pathway.AddRectangle(coord.Key.Item1,
                                            coord.Key.Item2, false, 0.5, tooltip, koIds));
                                    }

                                    selectedPaths.Add(pathway);
                                    Thread.Sleep(500);
                                }
                            }
                        }
                    }
                    SelectedPathways = new ObservableCollection<Pathway>(selectedPaths);
                    SelectedPathway = selectedPaths.First();
                    _priorOrg = SelectedOrganism.Name;
                }
                IsQuerying = false;
                PathwayTabIndex = 0;
            }));
        }
        
        /// <summary>
        /// Method to begin the download of FASTA file for an organism
        /// </summary>
        /// <param name="currentOrg">Organism that the user has selected</param>
        private void StartFastaDownloads(Organism currentOrg)
        {
            // To prevent concurrent calls
            while (_ncbiDownloading)
            {
                continue;
            }
            try
            {
                _ncbiDownloading = true;
                // Query DB to get ftp location for organism
                var ftpLoc = GetFtpLocationFromDb(currentOrg.OrgCode);

                // Connect to ftp site at above location to get all the .faa files
                var filesForOrg = GetFtpFileList(ftpLoc);

                // For each .faa and .fa.gz file...

                foreach (var file in filesForOrg)
                {
                    if ((file.EndsWith(".faa") || file.EndsWith(".fa.gz")) && !_parsedFiles.Contains(file))
                    {
                        // Download the file and parse it
                        var tempFileLoc = DownloadFaaFile(ftpLoc + file);
                        ParseFaaFile(tempFileLoc);
                        _parsedFiles.Add(file);
                    }
                }
                _ncbiDownloading = false;
            }
            catch (Exception)
            {
                _ncbiDownloading = false;
                _errorFound = true;
                TopLevelWindow = 2;

                ErrorMessage = "ERROR: Unable to establish connection to NCBI to acquire FASTA for your organisms.";

                //ErrorDetail = "HERP DERP DO!!!!";
                
                NcbiSolution = Visibility.Visible;
            }
        }

        /// <summary>
        /// Database Query to determine the FTP location for the organism based on OrgCode
        /// </summary>
        /// <param name="orgCode">Kegg Org Code of interested</param>
        /// <returns>Full FTP location for the organism passed in</returns>
        private string GetFtpLocationFromDb(string orgCode)
        {
            var fileLoc = "";
            using (var dbConnection = new SQLiteConnection("Datasource=" + _dbPath + ";Version=3;"))
            {
                dbConnection.Open();
                using (var cmd = new SQLiteCommand(dbConnection))
                {
                    var selection = " SELECT ncbi_org_location FROM orgFaaLocation WHERE kegg_org_code = \"" + orgCode + "\"; ";
                    cmd.CommandText = selection;
                    var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        fileLoc = reader.GetString(0);
                    }

                }
            }
            return fileLoc;
        }

        /// <summary>
        /// Method to download files from NCBI's FTP server
        /// If this is a .gz file, it makes sure to persist the extension
        /// </summary>
        /// <param name="fileSource">FTP address of file to download</param>
        /// <returns>Temporary file location that the file is saved in</returns>
        private string DownloadFaaFile(string fileSource)
        {
            const string nihUserName = "anonymous";
            const string nihPassword = "michael.degan@pnnl.gov";
            const int bufferLength = 2048;

            // Set up the FTP connection and settings
            var reqFtp = (FtpWebRequest)WebRequest.Create(new Uri(fileSource));
            reqFtp.Credentials = new NetworkCredential(nihUserName, nihPassword);
            reqFtp.KeepAlive = false;
            reqFtp.Method = WebRequestMethods.Ftp.DownloadFile;
            reqFtp.UseBinary = true;
            reqFtp.Proxy = null;
            reqFtp.UsePassive = true;
            reqFtp.Timeout = 15000;
            var tempFileLoc = "";

            var response = (FtpWebResponse)reqFtp.GetResponse();

            using (var responseStream = response.GetResponseStream())
            {
                if (responseStream == null)
                {
                    Console.WriteLine("Response is empty");
                    return tempFileLoc;
                }

                var outputFilePath = Path.GetTempFileName();
                if (fileSource.EndsWith(".gz"))
                {
                    outputFilePath += ".gz";
                }
                tempFileLoc = outputFilePath ;
                using (var outFile = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {

                    var buffer = new Byte[bufferLength];
                    var bytesRead = responseStream.Read(buffer, 0, bufferLength);
                    while (bytesRead > 0)
                    {
                        outFile.Write(buffer, 0, bytesRead);
                        bytesRead = responseStream.Read(buffer, 0, bufferLength);
                    }
                }

            }
            return tempFileLoc;
        }

        /// <summary>
        /// Method to return a list of all files at an NCBI ftp location
        /// that user is interested in
        /// </summary>
        /// <param name="url">Top level location of files at URL</param>
        /// <returns>List of all file names in the folder of interest (not full paths)</returns>
        private List<string> GetFtpFileList(string url)
        {
            var downloadFiles = new List<string>();
            var result = new System.Text.StringBuilder();
            const string nihUserName = "anonymous";
            const string nihPassword = "michael.degan@pnnl.gov";

            try
            {
                Console.WriteLine("Examining " + url);

                var reqFtp = (FtpWebRequest)WebRequest.Create(new Uri(url));
                reqFtp.UseBinary = true;
                reqFtp.Credentials = new NetworkCredential(nihUserName, nihPassword);
                reqFtp.Method = "LIST";
                reqFtp.Proxy = null;
                reqFtp.KeepAlive = true;
                reqFtp.UsePassive = true;
                reqFtp.Timeout = 15000;
                using (var webResponse = (FtpWebResponse)reqFtp.GetResponse())
                {
                    var response = webResponse.GetResponseStream();
                    if (response == null)
                    {
                        Console.WriteLine("No files found for {0}", url);
                        return downloadFiles;
                    }

                    using (var responseReader = new StreamReader(response))
                    {
                        while (responseReader.Peek() > -1)
                        {
                            var line = responseReader.ReadLine();
                            if (string.IsNullOrWhiteSpace(line))
                                continue;

                            result.Append(line.Split(' ').Last());
                            result.Append("\n");
                        }
                    }
                }

                var lastLinefeed = result.ToString().LastIndexOf('\n');

                if (lastLinefeed > 0)
                    result.Remove(lastLinefeed, 1);

                downloadFiles = result.ToString().Split('\n').ToList();

            }
            catch (WebException wEx)
            {
                if (wEx.Message.Contains("(450)"))
                {
                    // Folder not found
                    Console.WriteLine("Folder not found {0}", url);
                }
                else
                {
                    Console.WriteLine(wEx.Message);
                    Console.WriteLine("No files found for {0}", url);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("No files found for {0}", url);
            }

            return downloadFiles;
        }

        /// <summary>
        /// Method to parse a fasta file, linking the fasta information to refSeq accessions
        /// </summary>
        /// <param name="faaFileLocation">File location for the .faa or .fa.gz file</param>
        private void ParseFaaFile(string faaFileLocation)
        {
            // If file is gzipped, file needs to unzip and update the file location
            if (faaFileLocation.EndsWith(".gz"))
            {
                var success = UnGzipFile(faaFileLocation);
                if (success)
                {
                    File.Delete(faaFileLocation);
                    faaFileLocation = faaFileLocation.Substring(0, faaFileLocation.Length - 3);
                }
            }

            var accKey = "";
            
            
            using (var reader = new StreamReader(new FileStream(faaFileLocation, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                while (reader.Peek() > -1)
                {
                    var readLine = reader.ReadLine();

                    //If the line is empty, pass over it
                    if (string.IsNullOrWhiteSpace(readLine))
                        continue;

                    //If the line is the start of information for new accession
                    if (readLine.StartsWith(">"))
                    {
                        // Splits the .faa line into the relevant pieces:
                        // Only piece we desire is the portion with the accession
                        // We do not care about the version of the accession due to data returned
                        // from the database, so we strip of that portion.
                        char[] separators = {'>', '|', '[', ']'};
                        var linePieces = readLine.Split(separators);
                        accKey = linePieces[4].Split('.')[0];
                        if (!_ncbiFastaDictionary.ContainsKey(accKey))
                        {
                            _ncbiFastaDictionary.Add(accKey, "");
                        }
                    }
                    //Add the line to the accession we are currently working on
                    _ncbiFastaDictionary[accKey] += readLine + '\n';
                }
            }
            // Clean up temporary file as it's no longer necessary
            File.Delete(faaFileLocation);
        }

        /// <summary>
        /// Method to unzip a file, used for if the file is a .fa.gz
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>True if the file was successfully unzipped, false otherwise</returns>
        private static bool UnGzipFile(string filePath)
        {
            var fiFile = new FileInfo(filePath);
            if (fiFile.DirectoryName == null)
            {
                Console.WriteLine("Folder info not available for " + filePath);
                return false;
            }

            if (fiFile.Extension.ToLower() != ".gz")
            {
                Console.WriteLine("Not a GZipped file; must have extension .gz: " + fiFile.FullName);
                return false;
            }

            var fileName = fiFile.Name;
            var decompressedFilePath = Path.Combine(fiFile.DirectoryName, fileName.Remove(fileName.Length - fiFile.Extension.Length));

            using (FileStream inFile = fiFile.OpenRead())
            using (GZipStream gzStream = new GZipStream(inFile, CompressionMode.Decompress))
            {
                // Create the decompressed file.
                using (FileStream outFile = File.Create(decompressedFilePath))
                {
                    gzStream.CopyTo(outFile);
                }
            }

            return true;
        }

        /// <summary>
        /// Method to load the accessions that a user is interested in based on Pathways selected
        /// and kegg orthologs desired from their selections, and then creating a association between
        /// both the organism and the pathway
        /// </summary>
        private void AcquireProteins()
        {
            SelectedTabIndex = 4;
            string[] queryingStrings =
			    {
				    "Acquiring Genes   \nPlease Wait",
				    "Acquiring Genes.  \nPlease Wait",
				    "Acquiring Genes.. \nPlease Wait",
				    "Acquiring Genes...\nPlease Wait"
			    };
            QueryString = queryingStrings[0];
            
            var dataAccess = new DatabaseDataLoader(_dbPath);

            Task.Factory.StartNew(() =>
            {
                Task.Factory.StartNew(() => StartOverlay(queryingStrings));

                var selectedPaths = SelectedPathways.ToList();
                if (SelectedPathway != null && SelectedOrganism != null)
                {
                    // Load accessions for the pathway based on the selected proteins
                    foreach (var pathway in selectedPaths)
                    {
                        // Current flow is that exporting
                        // accessions requires a list of pathways
                        var temp = new List<Pathway> { pathway }; 
                        var pathwayAcc = dataAccess.ExportAccessions(temp, SelectedOrganism);

                        var association = new OrganismPathwayProteinAssociation
                        {
                            Pathway = pathway.Name,
                            Organism = SelectedOrganism.Name,
                            GeneList = new ObservableCollection<ProteinInformation>()
                        };

                        foreach (var acc in pathwayAcc)
                        {
                            association.GeneList.Add(acc);
                        }

                        // Create an association for the pathway/organism pair
                        AddAssociation(association);
                        IsPathwaySelected = true;
                    }
                }
                else
                {
                    MessageBox.Show("Please select an organism and pathway.");
                }
                IsQuerying = false;
            });
        }

        /// <summary>
        /// Reset the tracking information, to prep for a new organism.
        /// This does NOT clear the list of proteins that have been
        /// selected by the user, it just clears the pathway information
        /// and the selected organism from prior
        /// </summary>
        private void SelectAdditionalOrganism()
        {
            SelectedTabIndex = 1;
            SelectedOrganism = null;
            SelectedValue = "";
            FilteredProteins.Clear();

            foreach (var pathwayCatagory in Pathways)
            {
                foreach (var pathwayGroup in pathwayCatagory.PathwayGroups)
                {
                    foreach (var pathway in pathwayGroup.Pathways)
                    {
                        pathway.Selected = false;
                        pathway.SelectedKo.Clear();
                        if (pathway.PathwayImage != null)
                        {
                            pathway.PathwayDataCanvas.Children.Clear();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Async Method used to begin the animated overlay for when application is processing.
        /// During this method, tabs are unresponsive to prevent user from improper use of app.
        /// Due to the Async nature of the method, it should only be used in its own thread as
        /// the method will continue running until the IsQuerying property changes back to false
        /// </summary>
        /// <param name="overlayMessages">String array of messages to display inside the overlay</param>
        private void StartOverlay(string[] overlayMessages)
        {
            IsQuerying = true;

            int index = 0;
            int maxIndex = overlayMessages.Count();

            // Place holders for the boolean "enabled" on all tabs
            var pathTab = PathwaysTabEnabled;
            var selectTab = SelectionTabEnabled;
            var reviewTab = ReviewTabEnabled;
            var overviewTab = OverviewEnabled;
            // Disable all tabs to prevent misuse of app during processing
            PathwaysTabEnabled = false;
            SelectionTabEnabled = false;
            ReviewTabEnabled = false;
            OverviewEnabled = false;
            Thread.Sleep(500);
            while (IsQuerying && !_errorFound)
            {
                // Cycle through the messages passed in
                if (FileManager.Percentage > 0)
                {
                    QueryString = overlayMessages[index % maxIndex] + "\nDownload " + FileManager.Percentage.ToString() +
                                  "% complete";
                }
                else
                {
                    QueryString = overlayMessages[index % maxIndex];
                }
                index++;
                Thread.Sleep(500);
            }
            // Revery enabled status for all tabs.
            PathwaysTabEnabled = pathTab;
            SelectionTabEnabled = selectTab;
            ReviewTabEnabled = reviewTab;
            OverviewEnabled = overviewTab;
            PathwayTabIndex = 0;
        }

        /// <summary>
        /// Gathers all the protein accessions from the associations that have been selected
        /// by the user and uses the NCBI web API to create a FASTA file based on these.
        /// If there are no accessions, a message comes back saying so.
        /// </summary>
        private void ExportToSkyline()
        {
            //Clear the prior Proteins to export!!
            FilteredProteins.Clear();
            ProteinsToExport.Clear();
            var allFastas = "";

            // Allow the user to 
            string spectralLibPath;
            FolderBrowserDialog spectralLibPathDialog = new FolderBrowserDialog();
            spectralLibPathDialog.Description = "Select folder to save spectral library.";
            if (spectralLibPathDialog.ShowDialog() == DialogResult.OK)
            {
                spectralLibPath = spectralLibPathDialog.SelectedPath;
            }
            else
            {
                var errorMessage =
                            "No folder selected for spectral library.";
                MessageBox.Show(errorMessage, "Export Cancelled", MessageBoxButton.OK);
                return;
            }

            SelectedTabIndex = 4;
            string[] queryingStrings =
			    {
				    "Generating Fasta   \nPlease Wait",
				    "Generating Fasta.  \nPlease Wait",
				    "Generating Fasta.. \nPlease Wait",
				    "Generating Fasta...\nPlease Wait"
			    };
            QueryString = queryingStrings[0];
            
            Task.Factory.StartNew(() => StartOverlay(queryingStrings));

            Task.Factory.StartNew(() =>
            {

                // Go through the associations that have been built up so far...
                foreach (var association in PathwayProteinAssociation)
                {
                    // Create a list of all the genes from all the associations selected for export
                    if (association.AssociationSelected)
                    {
                        if (FilteredProteins == null)
                            FilteredProteins = new ObservableCollection<ProteinInformation>(association.GeneList);
                        else
                        {
                            foreach (var acc in association.GeneList)
                            {
                                FilteredProteins.Add(acc);
                            }
                        }
                    }
                }

                // Filter these genes from last step to eliminate duplicate
                foreach (var protein in FilteredProteins)
                {
                    if (!ProteinsToExport.Contains(protein.Accession) && protein.Selected)
                    {
                        ProteinsToExport.Add(protein.Accession);
                    }
                }


                // Create a list of just the accessions from the proteins to export
                var accessionList = ProteinsToExport.ToList();
                var accessionString = String.Join("+OR+", accessionList);

                // Continue waiting for the downloading to be complete.
                while (_ncbiDownloading)
                {
                    continue;
                }

                
                // Need to see if there are any NCBI accessions to pull and use this to 
                // create the FASTA file for skyline.
                if (!string.IsNullOrWhiteSpace(accessionString))
                {
                    foreach (var acc in accessionList)
                    {
                        if (!_accessionsWithFastaErrors.Contains(acc))
                        {
                            if (_ncbiFastaDictionary.ContainsKey(acc))
                            {
                                allFastas += _ncbiFastaDictionary[acc] + '\n';
                            }
                            else
                            {
                                _accessionsWithFastaErrors.Add(acc);
                            }
                        }
                    }
                    if (ToolClient != null)
                    {
                        // If we're using skyline interaction, push that data to Skyline
                        ToolClient.ImportFasta(allFastas);
                    }
                    var errors = string.Join("\n", _accessionsWithFastaErrors);
                    // Display to the user the errors that occurred during fasta construction
                    if (!string.IsNullOrEmpty(errors))
                    {
                        using (var errorWriter = new StreamWriter(@"C:\Temp\BioDiversityPluginNCBIErrors.txt"))
                        {
                            errorWriter.WriteLine(errors);
                        }
                        if (_accessionsWithFastaErrors.Count > 20)
                        {
                            errors = string.Join("\n", _accessionsWithFastaErrors.GetRange(0, 20)) + "\n...\n";
                        }
                        MessageBox.Show("NCBI server unreachable for the following accessions:\n" + errors + "\nFull list saved to: C:\\Temp\\BioDiversityPluginNCBIErrors.txt",
                                "NCBI Server Unreachable",
                                MessageBoxButton.OK);

                    }
                }
                else
                {
                    var confirmationMessage = "No NCBI accessions given, no FASTA file created.";

                    MessageBox.Show(confirmationMessage, "FASTA unable to be created", MessageBoxButton.OK,
                        MessageBoxImage.Exclamation);
                }

                IsQuerying = false;

                // IMPORTANT! This sleep length is so that the overlay can "refresh" to the proper information to show to the user
                Thread.Sleep(501);

                //Create list of organisms to use with the downloader below.
                List<string> organismList = new List<string>();

                //Have loops to pull just the organism name to put into the organism list.
                foreach (var association in PathwayProteinAssociation)
                {
                    if (association.AssociationSelected && !organismList.Contains(association.Organism))
                    {
                        organismList.Add(association.Organism);
                    }
                }
                var dataImported = true;

                //This is a loop to download the .blib files for each organism selected.
                foreach (var org in organismList)
                {
                    string[] downloadingStrings = {
				    "Downloading Spectral Library   \nPlease Wait",
				    "Downloading Spectral Library.  \nPlease Wait",
				    "Downloading Spectral Library.. \nPlease Wait",
				    "Downloading Spectral Library...\nPlease Wait"
			        };
                    QueryString = downloadingStrings[0];
                    Task.Factory.StartNew(() => StartOverlay(downloadingStrings));
                    var fileFound = false;
                    var fileLoc = CheckFileLocation(org);
                    if (!string.IsNullOrWhiteSpace(fileLoc))
                    {
                        //Check if the file is already downloaded and saved.
                        if (File.Exists(fileLoc))
                        {
                            fileFound = true;
                            //Show where the file was already found at
                            MessageBox.Show("Spectral Library was already found saved to " + fileLoc);
                            if (ToolClient != null)
                            {
                                // If we're using the Skyline connection, push the downloaded .blib to Skyline
                                ToolClient.AddSpectralLibrary(org + " Spectral Library", fileLoc);
                            }
                        }
                    }
                    //File hasn't been downloaded yet from the massive server for this organism
                    if (!fileFound)
                    {
                        var dlFailed = false;
                        string bestFile = "";
                        // Attempt to access the anonymous user MassIVE link
                        if (!org.StartsWith("Homo sapiens"))
                        {
                            try
                            {
                                var reqFtp =
                                    (FtpWebRequest)
                                        WebRequest.Create(new Uri("ftp://massive.ucsd.edu/MSV000079053/library/"));
                                reqFtp.UseBinary = true;
                                reqFtp.Method = "LIST";
                                reqFtp.Proxy = null;
                                reqFtp.KeepAlive = true;
                                reqFtp.UsePassive = true;
                                reqFtp.Timeout = 15000;

                                var files = new List<string>();

                                using (var webResponse = (FtpWebResponse) reqFtp.GetResponse())
                                {
                                    var response = webResponse.GetResponseStream();
                                    if (response == null)
                                    {
                                        Console.WriteLine(
                                            "No files found for ftp://massive.ucsd.edu/MSV000079053/library/");
                                        break;
                                    }
                                    using (var responseReader = new StreamReader(response))
                                    {
                                        while (responseReader.Peek() > -1)
                                        {
                                            var line = responseReader.ReadLine();
                                            if (string.IsNullOrWhiteSpace(line))
                                                continue;

                                            files.Add(line.Split(' ').Last());
                                        }
                                    }
                                }

                                int minDistance = 99;
                                //Loop to call the Levenshtein distance to find the best match
                                foreach (var file in files)
                                {
                                    int distance = LevenshteinDistance.Compute(org, file);
                                    if (distance < minDistance)
                                    {
                                        minDistance = distance;
                                        bestFile = file;
                                    }
                                }
                                //Finally, download the best file that we found for the organism.
                                var result = true;
                                if (!File.Exists(spectralLibPath + org.Replace(" ", "_") + ".blib"))
                                {
                                    //Combining the path of the massive server (with username/password encoded) with the name of the file
                                    result =
                                        FileManager.DownloadFile(
                                            ("ftp://massive.ucsd.edu/MSV000079053/library/" + bestFile + "/" + bestFile +
                                             ".blib"),
                                            (spectralLibPath));
                                }
                                else
                                {
                                    bestFile = org.Replace(" ", "_");
                                }
                                if (result)
                                {
                                    AddFileLocation(org, spectralLibPath + "\\" + bestFile + ".blib");
                                    MessageBox.Show("Spectral Library saved to " + spectralLibPath + "\\" + bestFile +
                                                    ".blib");
                                    if (ToolClient != null)
                                    {

                                        ToolClient.AddSpectralLibrary(org + " Spectral Library",
                                            spectralLibPath + "\\" + bestFile + ".blib");
                                    }
                                }
                            }
                            catch (WebException ex)
                            {
                                dlFailed = true;
                            }
                            // Attempt to access the Username based MassIVE link
                            if (dlFailed)
                            {
                                try
                                {

                                    var reqFtp =
                                        (FtpWebRequest) WebRequest.Create(new Uri("ftp://massive.ucsd.edu/library/"));
                                    reqFtp.UseBinary = true;
                                    reqFtp.Credentials = new NetworkCredential("MSV000079053", "a");
                                    reqFtp.Method = "LIST";
                                    reqFtp.Proxy = null;
                                    reqFtp.KeepAlive = true;
                                    reqFtp.UsePassive = true;
                                    reqFtp.Timeout = 15000;

                                    var files = new List<string>();

                                    using (var webResponse = (FtpWebResponse) reqFtp.GetResponse())
                                    {
                                        var response = webResponse.GetResponseStream();
                                        if (response == null)
                                        {
                                            Console.WriteLine("No files found for ftp://massive.ucsd.edu/library/");
                                            break;
                                        }
                                        using (var responseReader = new StreamReader(response))
                                        {
                                            while (responseReader.Peek() > -1)
                                            {
                                                var line = responseReader.ReadLine();
                                                if (string.IsNullOrWhiteSpace(line))
                                                    continue;

                                                files.Add(line.Split(' ').Last());
                                            }
                                        }
                                    }

                                    int minDistance = 99;
                                    //Loop to call the Levenshtein distance to find the best match
                                    foreach (var file in files)
                                    {
                                        int distance = LevenshteinDistance.Compute(org, file);
                                        if (distance < minDistance)
                                        {
                                            minDistance = distance;
                                            bestFile = file;
                                        }
                                    }
                                    //Finally, download the best file that we found for the organism.
                                    var result = true;
                                    if (!File.Exists(spectralLibPath + org.Replace(" ", "_") + ".blib"))
                                    {
                                        //Combining the path of the massive server (with username/password encoded) with the name of the file
                                        result =
                                            FileManager.DownloadFile(
                                                ("ftp://MSV000079053:a@massive.ucsd.edu/library/" + bestFile + "/" +
                                                 bestFile +
                                                 ".blib"),
                                                (spectralLibPath));
                                    }
                                    else
                                    {
                                        bestFile = org.Replace(" ", "_");
                                    }
                                    if (result)
                                    {
                                        AddFileLocation(org, spectralLibPath + "\\" + bestFile + ".blib");
                                        MessageBox.Show("Spectral Library saved to " + spectralLibPath + "\\" + bestFile +
                                                        ".blib");
                                        if (ToolClient != null)
                                        {

                                            ToolClient.AddSpectralLibrary(org + " Spectral Library",
                                                spectralLibPath + "\\" + bestFile + ".blib");
                                        }
                                        dlFailed = false;
                                    }
                                }
                                catch (Exception)
                                {
                                    dlFailed = true;
                                }
                            }
                        }
                        // LAST DITCH ATTEMPT: look on the PNL ftp for the blib.
                        if (dlFailed || org.StartsWith("Homo sapiens"))
                        {
                            try
                            {
                                var reqFtp =
                                    (FtpWebRequest)
                                        WebRequest.Create(new Uri("ftp://ftp.pnl.gov/outgoing/BiodiversityLibrary/"));
                                reqFtp.UseBinary = true;
                                reqFtp.Credentials = new NetworkCredential("proteomics", "Amt23Data");
                                reqFtp.Method = "LIST";
                                reqFtp.Proxy = null;
                                reqFtp.KeepAlive = true;
                                reqFtp.UsePassive = true;
                                reqFtp.Timeout = 15000;

                                var files = new List<string>();

                                using (var webResponse = (FtpWebResponse) reqFtp.GetResponse())
                                {
                                    var response = webResponse.GetResponseStream();
                                    if (response == null)
                                    {
                                        Console.WriteLine("No files found for ftp://massive.ucsd.edu/library/");
                                        break;
                                    }
                                    using (var responseReader = new StreamReader(response))
                                    {
                                        while (responseReader.Peek() > -1)
                                        {
                                            var line = responseReader.ReadLine();
                                            var opts = StringSplitOptions.RemoveEmptyEntries;
                                            var delims = new string[] {" "};
                                            var pieces = line.Split(delims, opts);
                                            if (string.IsNullOrWhiteSpace(line))
                                                continue;

                                            files.Add(line.Split(' ').Last());
                                        }
                                    }
                                }

                                int minDistance = 99;
                                //Loop to call the Levenshtein distance to find the best match
                                foreach (var file in files)
                                {
                                    int distance = LevenshteinDistance.Compute(org, file);
                                    if (distance < minDistance)
                                    {
                                        minDistance = distance;
                                        bestFile = file;
                                    }
                                }
                                //Finally, download the best file that we found for the organism.
                                var result = true;
                                if (!File.Exists(Path.Combine(spectralLibPath, bestFile)))
                                {
                                    //Combining the path of the massive server (with username/password encoded) with the name of the file
                                    result =
                                        FileManager.DownloadFile(
                                            ("ftp://ftp.pnl.gov/outgoing/BiodiversityLibrary/" + bestFile),
                                            (spectralLibPath), inNetwork: true);
                                }
                                if (result)
                                {
                                    AddFileLocation(org, spectralLibPath + "\\" + bestFile);
                                    MessageBox.Show("Spectral Library saved to " + spectralLibPath + "\\" + bestFile);
                                    if (ToolClient != null)
                                    {

                                        ToolClient.AddSpectralLibrary(org + " Spectral Library",
                                            spectralLibPath + "\\" + bestFile);
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                TopLevelWindow = 2;

                                MassiveSolution = Visibility.Visible;
                                ErrorMessage = "MassIVE Server unreachable";
                                ErrorDetail = "Unable to download .blib for " + org;

                                _errorFound = true;

                                //MessageBox.Show(
                                //    "MassIVE Server unreachable; Unable to download .blib for " + org +
                                //    "\nPlease check network connection and try again.", "MassIVE Server Unreachable",
                                //    MessageBoxButton.OK);
                                dataImported = false;
                            }
                        }
                    }
                }
                IsQuerying = false;
                //Thread.Sleep(501);
                //string[] importingStrings =
                //                    {
                //                        "Importing to Skyline   \nPlease Wait",
                //                        "Importing to Skyline.  \nPlease Wait",
                //                        "Importing to Skyline.. \nPlease Wait",
                //                        "Importing to Skyline...\nPlease Wait"
                //                    };
                //QueryString = importingStrings[0];
                //Task.Factory.StartNew(() => StartOverlay(importingStrings));

                if (ToolClient != null && dataImported)
                {
                    // Dispose of the client connection and move the user to the "Successful import view"
                    ToolClient.Dispose();
                    TopLevelWindow = 1;
                }
                IsQuerying = false;
            });
        }
        
        /// <summary>
        /// Short database script to check if the organism has downloaded a blib before
        /// </summary>
        /// <param name="orgName"></param>
        /// <returns>File location of the .blib</returns>
        private string CheckFileLocation(string orgName)
        {
            var fileLocSource = _dbPath.Replace("PBL.db", "blibFileLoc.db");
            var fileLoc = "";
            using (var dbConnection = new SQLiteConnection("Datasource=" + fileLocSource + ";Version=3;"))
            {
                dbConnection.Open();
                using (var cmd = new SQLiteCommand(dbConnection))
                {
                    var selectionText = " SELECT fileLocation FROM fileLocation WHERE orgName = \"" + orgName + "\"; ";
                    cmd.CommandText = selectionText;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            fileLoc = reader.GetString(0);
                        }
                    }
                }
            }
            return fileLoc;
        }

        /// <summary>
        /// Update the database with a linking of organism name to file location.
        /// This will not only add, but update the database with the proper location.
        /// </summary>
        /// <param name="orgName"></param>
        /// <param name="fileLoc"></param>
        private void AddFileLocation(string orgName, string fileLoc)
        {
            var fileLocSource = _dbPath.Replace("PBL.db", "blibFileLoc.db");

            using (var dbConnection = new SQLiteConnection("Datasource=" + fileLocSource + ";Version=3;"))
            {
                dbConnection.Open();
                using (var cmd = new SQLiteCommand(dbConnection))
                {
                    var deletionText = " DELETE FROM fileLocation WHERE orgName = \"" + orgName + "\"; ";
                    cmd.CommandText = deletionText;
                    cmd.ExecuteNonQuery();

                    var insertionText = " INSERT INTO fileLocation(orgName, fileLocation) VALUES ( ";
                    insertionText += "\"" + orgName + "\", \"" + fileLoc + "\" );";
                    cmd.CommandText = insertionText;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Method for adding an Association to the existing list
        /// </summary>
        /// <param name="newAssociation"></param>
        private void AddAssociation(OrganismPathwayProteinAssociation newAssociation)
        {
            var curList = PathwayProteinAssociation.ToList();
            var orgPathList = new Dictionary<string, List<string>>();
            foreach (var pair in curList)
            {
                if (!orgPathList.ContainsKey(pair.Organism))
                {
                    orgPathList.Add(pair.Organism, new List<string>());
                }
                orgPathList[pair.Organism].Add(pair.Pathway);
            }
            if (orgPathList.ContainsKey(newAssociation.Organism) &&
                orgPathList[newAssociation.Organism].Contains(newAssociation.Pathway))
            {
                var strippedTemp =
                    curList.Where(x => !(x.Organism == newAssociation.Organism && x.Pathway == newAssociation.Pathway));
                curList = new List<OrganismPathwayProteinAssociation>();
                foreach (var pair in strippedTemp)
                {
                    curList.Add(pair);
                }
            }
            curList.Add(newAssociation);
            PathwayProteinAssociation = new ObservableCollection<OrganismPathwayProteinAssociation>(curList);
        }

        public Visibility SkylineSolution { get { return _skylineSolution; } set { _skylineSolution = value; RaisePropertyChanged(); } }

        public string ErrorDetail { get { return _errorDetail; } set { _errorDetail = value; RaisePropertyChanged(); } }

        public string ErrorMessage { get { return _errorMessage; } set { _errorMessage = value; RaisePropertyChanged(); } }

        public Visibility NcbiSolution { get { return _ncbiSolution; } set { _ncbiSolution = value; RaisePropertyChanged(); } }

        public Visibility MassiveSolution { get { return _massiveSolution; } set { _massiveSolution = value; RaisePropertyChanged(); } }
    }
}