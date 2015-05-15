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

        #endregion

        #region Public Properties

        public SkylineToolClient ToolClient
        {
            get { return _toolClient; }
            private set { _toolClient = value; }
        }

        public int TopLevelWindow
        {
            get { return _topLevelWindow; }
            set
            {
                _topLevelWindow = value;
                RaisePropertyChanged();
            }
        }

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

                //ListPathwaySelected = false;
                //if (ListPathways.Contains(value))
                //{
                //    ListPathwaySelected = true;
                //}

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

        #endregion

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
            _versionBool = goodVersion; //TODO: Delete this after check

            if (ToolClient != null && !goodVersion)
            {
                TopLevelWindow = 2;
            }

        }

        private void CloseApplication()
        {
            Application.Current.Shutdown();
        }

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
            string[] queryingStrings =
			    {
				    "Determining Pathway Coverage   \nPlease Wait",
				    "Determining Pathway Coverage.  \nPlease Wait",
				    "Determining Pathway Coverage.. \nPlease Wait",
				    "Determining Pathway Coverage...\nPlease Wait"
			    };
            QueryString = queryingStrings[0];

            //Task.Factory.StartNew(() => StartOverlay(queryingStrings));

            Task.Factory.StartNew(() =>
            {
                var dataAccess = new DatabaseDataLoader(_dbPath);

                var pathList = (from catagory in Pathways 
                                from @group in catagory.PathwayGroups 
                                where @group.GroupName != "Global and Overview Maps" 
                                from path in @group.Pathways select path).ToList();

                //var pathList =
                //    (from catagory in Pathways
                //     from @group in catagory.PathwayGroups
                //     from pathway in @group.Pathways
                //     select pathway).ToList();

                foreach (var path in pathList)
                {
                    if (SelectedTabIndex == 2 || path.PercentCover < -1)
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


                //IsQuerying = false;
            });
        }

        private void ClearFilter()
        {
            SelectedValue = "";
        }

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

        private void SelectPathway()
        {
            var temp = SelectedPathwayTreeItem;
            SelectedPathwayTreeItem = temp;
        }

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

        private void PreviousTab()
        {
            if (SelectedTabIndex > 0)
            {
                SelectedTabIndex--;
            }
            if (SelectedTabIndex == 2)
            {
                LoadPathwayCoverage();
            }
        }

        private void NextTab()
        {
            // Do nothing if no selected organism and on the organism selection tab
            if (SelectedTabIndex == 1 && SelectedOrganism == null) return;
            // Do nothing if no selected pathway and on the pathway selection tab
            if (SelectedTabIndex == 2 && !IsPathwaySelected) return;
            SelectedTabIndex++;
        }

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
                                if (pathway.Selected)
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

        private void StartFastaDownloads(Organism currentOrg, List<Pathway> curPathways)
        {
            _ncbiDownloading = true;

            var dataLoader = new DatabaseDataLoader(_dbPath);
            var proteins = dataLoader.ExportAllOrgPathwayAccessions(currentOrg, curPathways);

            foreach (var protein in proteins)
            {
                if (!_ncbiFastaDictionary.ContainsKey(protein))
                {
                    try
                    {
                        var fasta = GetFastasFromNCBI(protein);
                        _ncbiFastaDictionary.Add(protein, fasta);
                    }
                    catch (Exception)
                    {
                        _accessionsWithFastaErrors.Add(protein);
                    }
                }
            }
            _ncbiDownloading = false;
        }

        private void StartFastaDownloads(Organism currentOrg)
        {
            var watch = new Stopwatch();
            watch.Start();
            _ncbiDownloading = true;
            // Query DB to get ftp location for organism
            var ftpLoc = GetFtpLocationFromDB(currentOrg.OrgCode);
            //var ftpLoc = "ftp://ftp.ncbi.nlm.nih.gov/genomes/Bacteria/Mycobacterium_tuberculosis_H37Rv_uid170532/";

            // Connect to ftp site at above location to get all the .faa files
            var filesForOrg =
                GetFtpFileList(ftpLoc);

            // For each .faa file
            foreach (var file in filesForOrg)
            {
                if (file.EndsWith(".faa") || file.EndsWith(".fa.gz"))
                {
                    var tempFileLoc = DownloadFaaFile(ftpLoc + file);
                    ParseFaaFile(tempFileLoc);
                }
            }
            // parse the fasta
            _ncbiDownloading = false;
            watch.Stop();
            Console.WriteLine(watch.Elapsed);
            Console.WriteLine(watch.ElapsedMilliseconds);
        }

        private string GetFtpLocationFromDB(string orgCode)
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

        private string DownloadFaaFile(string fileSource)
        {
            string NihUserName = "anonymous";
            string NihPassword = "michael.degan@pnnl.gov";
            int BUFFER_LENGTH = 2048;
            var reqFtp = (FtpWebRequest)WebRequest.Create(new Uri(fileSource));
            reqFtp.Credentials = new NetworkCredential(NihUserName, NihPassword);
            reqFtp.KeepAlive = false;
            reqFtp.Method = WebRequestMethods.Ftp.DownloadFile;
            reqFtp.UseBinary = true;
            reqFtp.Proxy = null;
            reqFtp.UsePassive = true;
            var tempFileLoc = "";

            var response = (FtpWebResponse)reqFtp.GetResponse();

            using (var responseStream = response.GetResponseStream())
            {
                if (responseStream == null)
                {
                    Console.WriteLine("Response is empty");
                    return tempFileLoc;
                }

                var outputFilePath = Path.GetTempFileName();// Path.Combine(destinationFolderPath, fileName);
                if (fileSource.EndsWith(".gz"))
                {
                    outputFilePath += ".gz";
                }
                tempFileLoc = outputFilePath ;
                using (var outFile = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {

                    var buffer = new Byte[BUFFER_LENGTH];
                    var bytesRead = responseStream.Read(buffer, 0, BUFFER_LENGTH);
                    while (bytesRead > 0)
                    {
                        outFile.Write(buffer, 0, bytesRead);
                        bytesRead = responseStream.Read(buffer, 0, BUFFER_LENGTH);
                    }
                }

            }
            return tempFileLoc;
        }

        private List<string> GetFtpFileList(string url)
        {
            var downloadFiles = new List<string>();
            var result = new System.Text.StringBuilder();
            string NihUserName = "anonymous";
            string NihPassword = "michael.degan@pnnl.gov";

            try
            {
                Console.WriteLine("Examining " + url);

                var reqFtp = (FtpWebRequest)WebRequest.Create(new Uri(url));
                reqFtp.UseBinary = true;
                reqFtp.Credentials = new NetworkCredential(NihUserName, NihPassword);
                reqFtp.Method = "LIST";
                reqFtp.Proxy = null;
                reqFtp.KeepAlive = true;
                reqFtp.UsePassive = true;
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

        private void ParseFaaFile(string faaFileLocation)
        {
            // while file open
                // if line begins with >
                    // parse the line to get the accession
                    // save accession (without the version, e.g. YP_01234 instead of YP_01234.5)
                    // add to accessionToFasta dict, accessionToFasta[key] = ""
                // add to accessionToFasta[key] += line
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
                    if (string.IsNullOrWhiteSpace(readLine))
                        continue;

                    if (readLine.StartsWith(">"))
                    {
                        // Splits the .faa line into the relevant pieces:
                        // piece 0 is empty, move ref|YP to the front followed by description
                        // with gi|1234 at the end
                        // Also splits out the organism name (enclosed in brackets)
                        // and does NOT enter it into the line.
                        char[] separators = { '>', '|', '[', ']' };
                        var linePieces = readLine.Split(separators);
                        accKey = linePieces[4].Split('.')[0];
                        _ncbiFastaDictionary.Add(accKey, "");
                    }
                    _ncbiFastaDictionary[accKey] += readLine;
                }
            }
            File.Delete(faaFileLocation);
        }

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

            // Need this for when the observable collections change.
            // The application level dispatcher needs to be utilized and through
            // dis.Invoke(() => <COMMAND TO EXECUTE> );
            var dis = Application.Current.Dispatcher;

            var dataAccess = new DatabaseDataLoader(_dbPath);

            Task.Factory.StartNew(() =>
            {
                Task.Factory.StartNew(() => StartOverlay(queryingStrings));

                var selectedPaths = SelectedPathways.ToList();
                //var accessions = new List<ProteinInformation>();
                if (SelectedPathway != null && SelectedOrganism != null)
                {
                    // Load accessions for the pathway based on the selected proteins
                    foreach (var pathway in selectedPaths)
                    {
                        var temp = new List<Pathway> { pathway }; // Current flow is that exporting
                        // accessions requires a list of pathways
                        // todo: CHANGE THIS TO USE SINGLE PATHWAY
                        var pathwayAcc = dataAccess.ExportAccessions(temp, SelectedOrganism);
                        //accessions.AddRange(pathwayAcc);

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
            while (IsQuerying)
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

            var dis = Application.Current.Dispatcher;

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

                while (_ncbiDownloading)
                {
                    continue;
                }

                var allFastas = "";
                // Need to see if there are any NCBI accessions to pull use to 
                // create the FASTA file.
                if (!string.IsNullOrWhiteSpace(accessionString))
                {
                    // Write the Fasta(s) from NCBI to file. This could eventually
                    // follow a different workflow depending on what Skyline needs.
                    //try
                    //{
                    //    GetFastasFromNCBI(accessionString);
                    //}
                    //catch (WebException)
                    //{
                    //    var errorMessage =
                    //        "Error accessing NCBI database\nPlease check your internet connection and try again.";
                    //    MessageBox.Show(errorMessage, "Error connecting to NCBI", MessageBoxButton.OK);
                    //}
                    //catch (Exception)
                    //{
                    //    var outputpath = "C:\\Temp\\accessionList.txt";
                    //    using (var fastaWriter = new StreamWriter(outputpath))
                    //    {
                    //        foreach (var acc in accessionList)
                    //        {
                    //            fastaWriter.WriteLine(acc);
                    //        }
                    //    }
                    //    var errorMessage =
                    //        "Error accessing NCBI database\nPlease check that the accessions are valid";
                    //    MessageBox.Show(errorMessage, "Error During creation", MessageBoxButton.OK);
                    //}

                    foreach (var acc in accessionList)
                    {
                        if (!_accessionsWithFastaErrors.Contains(acc))
                        {
                            allFastas += _ncbiFastaDictionary[acc] + '\n';
                        }
                    }
                    if (ToolClient != null)
                    {
                        ToolClient.ImportFasta(allFastas);
                    }
                    var errors = string.Join("\n", _accessionsWithFastaErrors);
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
                Thread.Sleep(501);


                string[] downloadingStrings = {
				    "Downloading Spectral Library   \nPlease Wait",
				    "Downloading Spectral Library.  \nPlease Wait",
				    "Downloading Spectral Library.. \nPlease Wait",
				    "Downloading Spectral Library...\nPlease Wait"
			    };
                QueryString = downloadingStrings[0];
                Task.Factory.StartNew(() => StartOverlay(downloadingStrings));


                //var something = new DatabaseDataLoader(_dbPath);
                //something.PeptidePuller(accessionList, "C:\\Temp\\peptideList.tsv");

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
                //This is a loop to use the Levenshtein class to find the closest file match to an organism name.
                foreach (var org in organismList)
                {
                    var fileFound = false;
                    var fileLoc = CheckFileLocation(org);
                    if (!string.IsNullOrWhiteSpace(fileLoc))
                    {
                        if (File.Exists(fileLoc))
                        {
                            fileFound = true;
                            MessageBox.Show("Spectral Library was already found saved to " + fileLoc);
                            if (ToolClient != null)
                            {
                                //Overlay so it says Importing to Skyline
                                IsQuerying = false;
                                Thread.Sleep(501);


                                string[] importingStrings =
                                {
                                    "Importing to Skyline   \nPlease Wait",
                                    "Importing to Skyline.  \nPlease Wait",
                                    "Importing to Skyline.. \nPlease Wait",
                                    "Importing to Skyline...\nPlease Wait"
                                };
                                QueryString = importingStrings[0];
                                Task.Factory.StartNew(() => StartOverlay(importingStrings));
                                //End

                                ToolClient.AddSpectralLibrary(org + " Spectral Library", fileLoc);
                            }
                        }
                    }
                    if(!fileFound)
                    {

                        string bestFile = "";
                        //var reqFtp = (FtpWebRequest)WebRequest.Create(new Uri("ftp://MSV000079053:a@massive.ucsd.edu/library/"));
                        try
                        {

                            var reqFtp = (FtpWebRequest)WebRequest.Create(new Uri("ftp://massive.ucsd.edu/library/"));
                            reqFtp.UseBinary = true;
                            reqFtp.Credentials = new NetworkCredential("MSV000079053", "a");
                            reqFtp.Method = "LIST";
                            reqFtp.Proxy = null;
                            reqFtp.KeepAlive = true;
                            reqFtp.UsePassive = true;

                            var files = new List<string>();

                            using (var webResponse = (FtpWebResponse)reqFtp.GetResponse())
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
                                        //result.Append("\n");
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
                                result =
                                    FileManager.DownloadFile(
                                        ("ftp://MSV000079053:a@massive.ucsd.edu/library/" + bestFile + "/" + bestFile +
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
                                    //Overlay so it says Importing to Skyline
                                    IsQuerying = false;
                                    Thread.Sleep(501);


                                    string[] importinStrings = {
				                    "Importing to Skyline   \nPlease Wait",
				                    "Importing to Skyline.  \nPlease Wait",
				                    "Importing to Skyline.. \nPlease Wait",
				                    "Importing to Skyline...\nPlease Wait"
			                        };
                                    QueryString = importinStrings[0];
                                    Task.Factory.StartNew(() => StartOverlay(importinStrings));
                                    //End

                                    ToolClient.AddSpectralLibrary(org + " Spectral Library",
                                        spectralLibPath + "\\" + bestFile + ".blib");
                                }
                            }
                        }
                        catch (Exception)
                        {
                            MessageBox.Show(
                                "MassIVE Server unreachable; Unable to download .blib for " + org +
                                "\nPlease check network connection and try again.", "MassIVE Server Unreachable",
                                MessageBoxButton.OK);
                            dataImported = false;
                        }
                    }
                }


                IsQuerying = false;

                if (ToolClient != null && dataImported)
                {
					/* Need to keep app open until skyline is done loading the .blib information */
					//TODO: See if Skyline can send us a message saying that it's done loading ^^^^
                    ToolClient.Dispose();
                    TopLevelWindow = 1;
                    //dis.InvokeShutdown();
                    //Application.Current.Shutdown();
                    //Environment.Exit(0);
                }
                //if (ToolClient != null)
                //{
                //    ToolClient.Dispose();
                //    ToolClient = null;
                //    //Application.Current.Shutdown();
                //}

            });
        }

        /// <summary>
        /// Use of the NCBI web API to get the FASTAs for the string of Accessions selected.
        /// Creates a FASTA formatted file, currently to C:\Temp\currentSelection.fasta
        /// </summary>
        /// <param name="accessionString">A list of NCBI accessions, separated by "+OR+" for use with NCBI</param>
        /// <returns>The FASTA for all accessions</returns>
        private string GetFastasFromNCBI(string accessionString)
        {
            var fastas = "";

            var esearchURL =
                "http://eutils.ncbi.nlm.nih.gov/entrez/eutils/efetch.fcgi?db=protein&id=" + accessionString + "&rettype=fasta&retmode=txt";//&usehistory=y";

            var esearchGetUrl = WebRequest.Create(esearchURL);

            var getStream = esearchGetUrl.GetResponse().GetResponseStream();
            var reader = new StreamReader(getStream);
            var streamLine = "";
            while (streamLine != null)
            {
                streamLine = reader.ReadLine();
                if (streamLine != null)
                {
                    fastas += streamLine + '\n';
                }
            }
            fastas = fastas.Replace("\n\n", "\n");

            var outputpath = "C:\\Temp\\currentSelection.fasta";

            if (File.Exists(outputpath))
            {
                File.Delete(outputpath);
            }

            using (var fastaWriter = new StreamWriter(outputpath))
            {
                fastaWriter.Write(fastas, 0, fastas.Length);
            }

            return fastas;
        }

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
    }
}