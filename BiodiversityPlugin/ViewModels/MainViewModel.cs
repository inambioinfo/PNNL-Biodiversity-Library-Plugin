﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using BiodiversityPlugin.DataManagement;
using BiodiversityPlugin.Models;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;

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
        private List<ProteinInformation> _proteinsToExport;
        private List<string> _organismList;
        private Visibility _filterVisibility;

        private bool _listPathwaySelected;
        private bool _isOrganismSelected;
        private bool _pathwaysTabEnabled;
        private bool _isPathwaySelected;
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
        private Visibility _queryingVisibility;
        private bool _overviewTabEnabled;

        #endregion

        #region Public Properties

        public ObservableCollection<OrgPhylum> Organisms { get; private set; }
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
            private set
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
            get { return _selectedOrganismText; }
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
                    SelectedOrganism = (Organism) _selectedOrganismTreeItem;
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

        public List<ProteinInformation> ProteinsToExport
        {
            get { return _proteinsToExport; }
            private set
            {
                _proteinsToExport = value;
                RaisePropertyChanged("ProteinsToExport");
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

        public Visibility QueryingVisibility
        {
            get
            {
                return _queryingVisibility;
            }
            set
            {
                _queryingVisibility = value;
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
                var filtered = (from phylum in Organisms
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
                RaisePropertyChanged("PathwayProteinAssociation");
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
                foreach (var phylum in Organisms)
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

        public MainViewModel(IDataAccess orgData, IDataAccess pathData, string dbPath)
        {
            _dbPath = dbPath;

            Messenger.Default.Register<PropertyChangedMessage<bool>>(this, PathwaysSelectedChanged);
            var organismList = new List<string>();
            var organisms = orgData.LoadOrganisms(ref organismList);

            organismList.Sort();
            OrganismList = organismList;
            organisms.Sort((x, y) => x.PhylumName.CompareTo(y.PhylumName));
            Organisms = new ObservableCollection<OrgPhylum>(organisms);
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

            _pathwayTabIndex = 0;
            _selectedTabIndex = 0;
            _isOrganismSelected = false;
            _isPathwaySelected = false;

            _pathwaysSelected = 0;
            ListPathways = new ObservableCollection<string>();

            _selectedPathways = new ObservableCollection<Pathway>();
            SelectedPathways = _selectedPathways;

            ProteinsToExport = new List<ProteinInformation>();
            PathwayProteinAssociation = new ObservableCollection<OrganismPathwayProteinAssociation>();
            SelectedValue = "";
            _priorOrg = "";
            QueryingVisibility = Visibility.Hidden;
            _overviewTabEnabled = true;
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

            IsQuerying = true;
            QueryingVisibility = Visibility.Visible;
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
            var currentOrg = SelectedOrganism.Name;
            var curPathways = new ObservableCollection<Pathway>((from pathwayCatagory in Pathways
                                                                 from @group in pathwayCatagory.PathwayGroups
                                                                 from p in @group.Pathways
                                                                 where p.Selected
                                                                 select p).ToList());
            var same = !(curPathways.Count != SelectedPathways.Count || 
                        (curPathways.Any(pathway => !SelectedPathways.Contains(pathway)) || 
                            SelectedPathways.Any(pathway => !curPathways.Contains(pathway))));

            // Need this for when anything in the canvas changes.
            // The application level dispatcher needs to be utilized and through
            // dis.Invoke(() => <COMMAND TO EXECUTE> );
            var dis = Application.Current.Dispatcher;

            Task.Factory.StartNew((() =>
            {

                int index = 0;

                var pathTab = PathwaysTabEnabled;
                var selectTab = SelectionTabEnabled;
                var reviewTab = ReviewTabEnabled;
                var overviewTab = OverviewEnabled;
                PathwaysTabEnabled = false;
                SelectionTabEnabled = false;
                ReviewTabEnabled = false;
                OverviewEnabled = false;

                while (IsQuerying)
                {
                    Thread.Sleep(750);
                    QueryString = queryingStrings[index % 4];
                    index++;
                }

                PathwaysTabEnabled = pathTab;
                SelectionTabEnabled = selectTab;
                ReviewTabEnabled = reviewTab;
                OverviewEnabled = overviewTab;
            }));

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
                                    pathway.LoadImage();

                                    // Remove any rectangles from the canvas to provide accurate visualization
                                    dis.Invoke(() =>
                                        pathway.ClearRectangles());

                                    // Create placeholder information for creating legend, KO is needed for adding
                                    // the rectangles
                                    var legend = new KeggKoInformation("legend", "legend", "legend");
                                    var legendList = new List<KeggKoInformation> { legend };

                                    // Draw the information for the Legend on each image.
                                    dis.Invoke(() =>
                                        pathway.AddRectangle(legendList, 10,
                                        5, false, Colors.Red));
                                    dis.Invoke(() =>
                                        pathway.WriteFoundText(10, 5, SelectedOrganism.Name));
                                    dis.Invoke(() =>
                                        pathway.AddRectangle(
                                        legendList, 10,
                                        22, false, Colors.Blue));
                                    dis.Invoke(() =>
                                        pathway.WriteNotfoundText(10, 22, SelectedOrganism.Name));

                                    // Now that we have the base image and the legend, load the coordinates
                                    // for every rectangle on the image, keyed on KO name.
                                    var koToCoordDict = //dis.Invoke(() =>
                                        pathway.LoadCoordinates();//);

                                    // Use the database to determine which orthologs have data in MSMS and load
                                    // the coordinates
                                    var koWithData = //dis.Invoke(() =>
                                        dataAccess.ExportKosWithData(pathway, SelectedOrganism);//);
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
                                        // Draw data rectangles for each of these coordinates
                                        // These rectangles are able to be interacted with by the user
                                        dis.Invoke(() =>pathway.AddRectangle(
                                            coord.Value, coord.Key.Item1,
                                            coord.Key.Item2, true));
                                    }

                                    // Do the same for orthologs without data in MSMS, loading the coordinates needed
                                    var koWithoutData = //dis.Invoke(() =>
                                        dataAccess.ExportKosWithoutData(pathway, SelectedOrganism);//);
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
                                        // Draw non-data rectangles for each of these coordinates
                                        // These rectangles have no interaction from the user
                                        dis.Invoke(() =>pathway.AddRectangle(
                                            coord.Value, coord.Key.Item1,
                                            coord.Key.Item2, false));
                                    }

                                    selectedPaths.Add(pathway);
                                }
                            }
                        }
                    }
                    SelectedPathways = new ObservableCollection<Pathway>(selectedPaths);
                    SelectedPathway = selectedPaths.First();
                    _priorOrg = SelectedOrganism.Name;
                }
                IsQuerying = false;
                QueryingVisibility = Visibility.Hidden;
                PathwayTabIndex = 0;
            }));
        }

        private void AcquireProteins()
        {
            IsQuerying = true;
            QueryingVisibility = Visibility.Visible;
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

            Task.Factory.StartNew(() =>
            {

                int index = 0;

                var pathTab = PathwaysTabEnabled;
                var selectTab = SelectionTabEnabled;
                var reviewTab = ReviewTabEnabled;
                var overviewTab = OverviewEnabled;
                PathwaysTabEnabled = false;
                SelectionTabEnabled = false;
                ReviewTabEnabled = false;
                OverviewEnabled = false;

                while (IsQuerying)
                {
                    Thread.Sleep(750);
                    QueryString = queryingStrings[index % 4];
                    index++;
                }

                PathwaysTabEnabled = pathTab;
                SelectionTabEnabled = selectTab;
                ReviewTabEnabled = reviewTab;
                OverviewEnabled = overviewTab;
            });

            var dataAccess = new DatabaseDataLoader(_dbPath);
            
            Task.Factory.StartNew(() =>
            {
                var selectedPaths = SelectedPathways.ToList();
                var accessions = new List<ProteinInformation>();
                if (SelectedPathway != null && SelectedOrganism != null)
                {
                    // Load accessions for the pathway based on the selected proteins
                    foreach (var pathway in selectedPaths)
                    {
                        var temp = new List<Pathway> { pathway }; // Current flow is that exporting
                        // accessions requires a list of pathways
                        // todo: CHANGE THIS TO USE SINGLE PATHWAY
                        var pathwayAcc = dataAccess.ExportAccessions(temp, SelectedOrganism);
                        accessions.AddRange(pathwayAcc);

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
                        dis.Invoke(() =>
                            AddAssociation(association));
                        IsPathwaySelected = true;
                    }
                }
                else
                {
                    MessageBox.Show("Please select an organism and pathway.");
                }
                IsQuerying = false;
                QueryingVisibility = Visibility.Hidden;
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
                            pathway.PathwayNonDataCanvas.Children.Clear();
                            pathway.PathwayDataCanvas.Children.Clear();
                        }
                    }
                }
            }
        }

        private void ExportToSkyline()
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
                if (!ProteinsToExport.Contains(protein))
                {
                    ProteinsToExport.Add(protein);
                }
            }

            // Create a list of just the accessions from the proteins to export
            var accessionList = ProteinsToExport.Select(protein => protein.Accession).ToList();

            var accessionString = String.Join("+OR+", accessionList);

            // Need to see if there are any NCBI accessions to pull use to 
            // create the FASTA file.
            if (!string.IsNullOrWhiteSpace(accessionString))
            {
                // Write the Fasta(s) from NCBI to file. This could eventually
                // follow a different workflow depending on what Skyline needs.
                var allFastas = GetFastasFromNCBI(accessionString);

                var confirmationMessage = "FASTA file for selected genes written to C:\\Temp\\fasta.txt";

                MessageBox.Show(confirmationMessage, "FASTA Created", MessageBoxButton.OK);
            }
            else
            {
                var confirmationMessage = "No NCBI accessions given, no FASTA file created.";

                MessageBox.Show(confirmationMessage, "FASTA unable to be created", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        /// <summary>
        /// Use of the NCBI web API to get the FASTAs for the string of Accessions selected.
        /// </summary>
        /// <param name="accessionString">A list of NCBI accessions, separated by "+OR+" for use with NCBI</param>
        /// <returns>The FASTA for all accessions</returns>
        private string GetFastasFromNCBI(string accessionString)
        {
            var fastas = "";

            var esearchURL =
                "http://eutils.ncbi.nlm.nih.gov/entrez/eutils/efetch.fcgi?db=nucleotide&id=" + accessionString + "&rettype=fasta&retmode=txt";//&usehistory=y";

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

            var outputpath = "C:\\Temp\\fasta.txt";

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

        /// <summary>
        /// Method for adding an Association to the existing list
        /// </summary>
        /// <param name="newAssociation"></param>
        private void AddAssociation(OrganismPathwayProteinAssociation newAssociation)
        {
            var curList = PathwayProteinAssociation;
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
                curList = new ObservableCollection<OrganismPathwayProteinAssociation>();
                foreach (var pair in strippedTemp)
                {
                    curList.Add(pair);
                }
            }
            curList.Add(newAssociation);
            PathwayProteinAssociation = curList;
        }
    }
}